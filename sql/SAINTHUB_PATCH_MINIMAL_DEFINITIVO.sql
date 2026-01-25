/* ============================================================
   SAINTHUB · PATCH MINIMAL DEFINITIVO (ZIP: SaintHub_definitivo_fix)
   DB: u484426513_ypudu
   - Idempotente (se puede correr varias veces)
   - Solo lo necesario para que el ZIP nuevo funcione
   - No borra tablas
   - No toca tipos de PriceCrc/SubtotalCrc (para no romper versiones viejas)
============================================================ */

USE `u484426513_ypudu`;
SET @db := DATABASE();

/* ---------------------------
   1) PRODUCTS: precios manuales (encargo)
--------------------------- */

-- OnDemandFixedPriceCrc
SET @sql := (
  SELECT IF(COUNT(*)=0,
    'ALTER TABLE `Products` ADD COLUMN `OnDemandFixedPriceCrc` INT NULL AFTER `PriceCrc`;',
    'SELECT 1;'
  )
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA=@db AND TABLE_NAME='Products' AND COLUMN_NAME='OnDemandFixedPriceCrc'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- OnDemandMinPriceCrc
SET @sql := (
  SELECT IF(COUNT(*)=0,
    'ALTER TABLE `Products` ADD COLUMN `OnDemandMinPriceCrc` INT NULL AFTER `OnDemandFixedPriceCrc`;',
    'SELECT 1;'
  )
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA=@db AND TABLE_NAME='Products' AND COLUMN_NAME='OnDemandMinPriceCrc'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- OnDemandMaxPriceCrc
SET @sql := (
  SELECT IF(COUNT(*)=0,
    'ALTER TABLE `Products` ADD COLUMN `OnDemandMaxPriceCrc` INT NULL AFTER `OnDemandMinPriceCrc`;',
    'SELECT 1;'
  )
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA=@db AND TABLE_NAME='Products' AND COLUMN_NAME='OnDemandMaxPriceCrc'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;


/* ---------------------------
   2) PRODUCTVARIANTS: Color + PriceCrc
--------------------------- */

-- Color
SET @sql := (
  SELECT IF(COUNT(*)=0,
    'ALTER TABLE `ProductVariants` ADD COLUMN `Color` VARCHAR(60) NOT NULL DEFAULT ''Default'' AFTER `Option`;',
    'SELECT 1;'
  )
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA=@db AND TABLE_NAME='ProductVariants' AND COLUMN_NAME='Color'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- PriceCrc
SET @sql := (
  SELECT IF(COUNT(*)=0,
    'ALTER TABLE `ProductVariants` ADD COLUMN `PriceCrc` INT NOT NULL DEFAULT 0 AFTER `Color`;',
    'SELECT 1;'
  )
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA=@db AND TABLE_NAME='ProductVariants' AND COLUMN_NAME='PriceCrc'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- Normalizar Color
SET SQL_SAFE_UPDATES = 0;
UPDATE `ProductVariants`
SET `Color` = 'Default'
WHERE `Color` IS NULL OR TRIM(`Color`) = '';
SET SQL_SAFE_UPDATES = 1;

-- Backfill PriceCrc (si quedó en 0)
SET SQL_SAFE_UPDATES = 0;
UPDATE `ProductVariants` pv
JOIN `Products` p ON p.Id = pv.ProductId
SET pv.PriceCrc = p.PriceCrc
WHERE pv.PriceCrc = 0;
SET SQL_SAFE_UPDATES = 1;


/* ---------------------------
   3) PRODUCTIMAGES: Color
--------------------------- */
SET @sql := (
  SELECT IF(COUNT(*)=0,
    'ALTER TABLE `ProductImages` ADD COLUMN `Color` VARCHAR(60) NULL AFTER `SortOrder`;',
    'SELECT 1;'
  )
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA=@db AND TABLE_NAME='ProductImages' AND COLUMN_NAME='Color'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;


/* ---------------------------
   4) ORDERS: UserId + TotalCrc INT
   - UserId: el ZIP lo usa
   - TotalCrc: si está DECIMAL, lo redondea y lo pasa a INT
     (esto NO debería romper versiones viejas: decimal -> int se puede leer como decimal)
--------------------------- */

-- UserId
SET @sql := (
  SELECT IF(COUNT(*)=0,
    'ALTER TABLE `Orders` ADD COLUMN `UserId` INT NULL AFTER `Id`;',
    'SELECT 1;'
  )
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA=@db AND TABLE_NAME='Orders' AND COLUMN_NAME='UserId'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- TotalCrc decimal -> int (solo si aplica)
SET @isDec := (
  SELECT COUNT(*)
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA=@db AND TABLE_NAME='Orders' AND COLUMN_NAME='TotalCrc'
    AND DATA_TYPE='decimal'
);

SET @sql := IF(@isDec > 0,
  'SET SQL_SAFE_UPDATES=0; UPDATE `Orders` SET `TotalCrc` = ROUND(`TotalCrc`); SET SQL_SAFE_UPDATES=1;',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

SET @sql := IF(@isDec > 0,
  'ALTER TABLE `Orders` MODIFY COLUMN `TotalCrc` INT NOT NULL;',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;


/* ---------------------------
   5) ORDERITEMS: FIX CRÍTICO
   - Si ProductId existe y es NOT NULL, lo hacemos NULL
     (si no, el insert falla porque el modelo nuevo NO lo llena)
   - Asegura columna Option
   - Copia OptionSelected -> Option si existe
--------------------------- */

-- ProductId NOT NULL -> NULL (si existe)
SET @sql := (
  SELECT CASE
    WHEN COUNT(*)=0 THEN 'SELECT 1;'
    WHEN MAX(IS_NULLABLE)='NO' THEN 'ALTER TABLE `OrderItems` MODIFY COLUMN `ProductId` INT NULL;'
    ELSE 'SELECT 1;'
  END
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA=@db AND TABLE_NAME='OrderItems' AND COLUMN_NAME='ProductId'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- Option (si no existe)
SET @sql := (
  SELECT IF(COUNT(*)=0,
    'ALTER TABLE `OrderItems` ADD COLUMN `Option` VARCHAR(100) NOT NULL DEFAULT '''' AFTER `ProductName`;',
    'SELECT 1;'
  )
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA=@db AND TABLE_NAME='OrderItems' AND COLUMN_NAME='Option'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- Copiar OptionSelected -> Option (si existe OptionSelected)
SET @hasOptSel := (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA=@db AND TABLE_NAME='OrderItems' AND COLUMN_NAME='OptionSelected'
);

SET @sql := IF(@hasOptSel > 0,
  'SET SQL_SAFE_UPDATES=0;
   UPDATE `OrderItems`
   SET `Option` = COALESCE(NULLIF(`OptionSelected`, ''''), `Option`)
   WHERE (TRIM(`Option`) = '''' OR `Option` IS NULL) AND `OptionSelected` IS NOT NULL;
   SET SQL_SAFE_UPDATES=1;',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;


/* ---------------------------
   6) USERS: Google + Reset (opcional, no rompe)
--------------------------- */

-- GoogleId
SET @sql := (
  SELECT IF(COUNT(*)=0,
    'ALTER TABLE `Users` ADD COLUMN `GoogleId` VARCHAR(255) NULL AFTER `PasswordHash`;',
    'SELECT 1;'
  )
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA=@db AND TABLE_NAME='Users' AND COLUMN_NAME='GoogleId'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- AuthProvider
SET @sql := (
  SELECT IF(COUNT(*)=0,
    'ALTER TABLE `Users` ADD COLUMN `AuthProvider` VARCHAR(50) NOT NULL DEFAULT ''Local'' AFTER `GoogleId`;',
    'SELECT 1;'
  )
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA=@db AND TABLE_NAME='Users' AND COLUMN_NAME='AuthProvider'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- ResetToken
SET @sql := (
  SELECT IF(COUNT(*)=0,
    'ALTER TABLE `Users` ADD COLUMN `ResetToken` VARCHAR(255) NULL AFTER `AuthProvider`;',
    'SELECT 1;'
  )
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA=@db AND TABLE_NAME='Users' AND COLUMN_NAME='ResetToken'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- ResetTokenExpires
SET @sql := (
  SELECT IF(COUNT(*)=0,
    'ALTER TABLE `Users` ADD COLUMN `ResetTokenExpires` DATETIME NULL AFTER `ResetToken`;',
    'SELECT 1;'
  )
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA=@db AND TABLE_NAME='Users' AND COLUMN_NAME='ResetTokenExpires'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;


/* ---------------------------
   7) ÍNDICES (performance)
--------------------------- */

-- ProductVariants(ProductId)
SET @sql := (
  SELECT IF(COUNT(*)=0,
    'CREATE INDEX IX_ProductVariants_ProductId ON ProductVariants(ProductId);',
    'SELECT 1;'
  )
  FROM information_schema.STATISTICS
  WHERE TABLE_SCHEMA=@db AND TABLE_NAME='ProductVariants' AND INDEX_NAME='IX_ProductVariants_ProductId'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- ProductImages(ProductId)
SET @sql := (
  SELECT IF(COUNT(*)=0,
    'CREATE INDEX IX_ProductImages_ProductId ON ProductImages(ProductId);',
    'SELECT 1;'
  )
  FROM information_schema.STATISTICS
  WHERE TABLE_SCHEMA=@db AND TABLE_NAME='ProductImages' AND INDEX_NAME='IX_ProductImages_ProductId'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- OrderItems(OrderId)
SET @sql := (
  SELECT IF(COUNT(*)=0,
    'CREATE INDEX IX_OrderItems_OrderId ON OrderItems(OrderId);',
    'SELECT 1;'
  )
  FROM information_schema.STATISTICS
  WHERE TABLE_SCHEMA=@db AND TABLE_NAME='OrderItems' AND INDEX_NAME='IX_OrderItems_OrderId'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;


SELECT 'SAINTHUB PATCH MINIMAL DEFINITIVO OK' AS Result;
