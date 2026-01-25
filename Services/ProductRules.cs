using SaintHub.Models;

namespace SaintHub.Services
{
    public static class ProductRules
    {
        // Category ids (must match navbar + admin selects)
        public const int Zapatos = 1;
        public const int Camisas = 2;
        public const int Suetas = 3;
        public const int Pantalones = 4;
        public const int Accesorios = 5;
        public const int Vinilos = 6;

        // Fulfillment ids
        public const int EnStock = 1;
        public const int PorEncargo = 2;

        public static bool IsVinyl(int category) => category == Vinilos;
        public static bool IsAccessory(int category) => category == Accesorios;

        public static string CategoryName(int category) => category switch
        {
            Zapatos => "Zapatos",
            Camisas => "Camisas",
            Suetas => "Suetas",
            Pantalones => "Pantalones",
            Accesorios => "Accesorios",
            Vinilos => "Vinilos",
            _ => "Categoría"
        };

        public static string OptionLabel(int category) => category switch
        {
            Accesorios => "Opción",
            Vinilos => "Formato",
            _ => "Talla"
        };

        /// <summary>
        /// Display price for cards/details.
        /// For stock: fixed. For on-demand: fixed or range when configured.
        /// </summary>
        public static (int fromCrc, int toCrc, bool isRange, string? note) GetDisplayPrice(Product p)
        {
            if (p.Fulfillment == EnStock)
                return (p.PriceCrc, p.PriceCrc, false, null);

            // Por encargo
            if (p.OnDemandFixedPriceCrc.HasValue && p.OnDemandFixedPriceCrc.Value > 0)
            {
                var v = p.OnDemandFixedPriceCrc.Value;
                return (v, v, false, "Precio confirmado desde admin");
            }

            var min = p.OnDemandMinPriceCrc;
            var max = p.OnDemandMaxPriceCrc;

            if (min.HasValue && max.HasValue)
            {
                var a = min.Value;
                var b = max.Value;
                if (a > b) (a, b) = (b, a);
                var isRange = a != b;
                return (a, b, isRange, isRange ? "Rango definido desde admin" : "Precio confirmado desde admin");
            }

            // Fallback: usar PriceCrc como estimado (sin +/− 15k)
            return (p.PriceCrc, p.PriceCrc, false, "Precio estimado (se confirma al coordinar) ");
        }

        /// <summary>
        /// Unit pricing to store in cart. If a range exists, UnitPriceCrc is set to max (estimate)
        /// and UnitPriceMin/Max are kept for display.
        /// </summary>
        public static (decimal unit, int? min, int? max) GetCartUnitPrice(Product p)
        {
            var (fromCrc, toCrc, isRange, _) = GetDisplayPrice(p);

            if (!isRange)
                return (fromCrc, fromCrc, fromCrc);

            return (toCrc, fromCrc, toCrc);
        }

        public static void NormalizeOnDemandPricing(Product p, string mode)
        {
            mode = (mode ?? "").Trim().ToLowerInvariant();

            if (p.Fulfillment != PorEncargo)
            {
                p.OnDemandFixedPriceCrc = null;
                p.OnDemandMinPriceCrc = null;
                p.OnDemandMaxPriceCrc = null;
                return;
            }

            if (mode == "fixed")
            {
                // if fixed isn't set, fall back to base price
                if (!p.OnDemandFixedPriceCrc.HasValue || p.OnDemandFixedPriceCrc.Value <= 0)
                    p.OnDemandFixedPriceCrc = p.PriceCrc;

                p.OnDemandMinPriceCrc = null;
                p.OnDemandMaxPriceCrc = null;
                return;
            }

            if (mode == "range")
            {
                // Ensure min/max exist
                var min = p.OnDemandMinPriceCrc ?? p.PriceCrc;
                var max = p.OnDemandMaxPriceCrc ?? p.PriceCrc;
                if (min > max) (min, max) = (max, min);

                p.OnDemandMinPriceCrc = min;
                p.OnDemandMaxPriceCrc = max;
                p.OnDemandFixedPriceCrc = null;
                return;
            }

            // If mode not specified, keep whatever is already set, but sanitize
            if (p.OnDemandFixedPriceCrc.HasValue && p.OnDemandFixedPriceCrc.Value <= 0)
                p.OnDemandFixedPriceCrc = null;

            if (p.OnDemandMinPriceCrc.HasValue && p.OnDemandMaxPriceCrc.HasValue)
            {
                var min = p.OnDemandMinPriceCrc.Value;
                var max = p.OnDemandMaxPriceCrc.Value;
                if (min > max) (min, max) = (max, min);
                p.OnDemandMinPriceCrc = min;
                p.OnDemandMaxPriceCrc = max;
            }
        }
    }
}
