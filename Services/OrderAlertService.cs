using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SaintHub.Data;
using System.Globalization;
using System.Text;

namespace SaintHub.Services
{
    public class OrderAlertService : IOrderAlertService
    {
        private readonly AppDbContext _db;
        private readonly IEmailSender _email;
        private readonly OrderAlertsOptions _opt;

        public OrderAlertService(AppDbContext db, IEmailSender email, IOptions<OrderAlertsOptions> opt)
        {
            _db = db;
            _email = email;
            _opt = opt.Value;
        }

        public async Task TryNotifyNewOrderAsync(int orderId)
        {
            try
            {
                if (!_opt.Enabled) return;
                if (_opt.Recipients == null || _opt.Recipients.Length == 0) return;

                var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
                if (order == null) return;

                var items = await _db.OrderItems.AsNoTracking()
                    .Where(i => i.OrderId == orderId)
                    .OrderBy(i => i.Id)
                    .ToListAsync();

                var crCulture = CultureInfo.GetCultureInfo("es-CR");
                string Money(decimal v) => string.Format(crCulture, "₡ {0:N0}", v);

                var subject = $"{_opt.SubjectPrefix} Nuevo pedido #{order.Id}";
                var sb = new StringBuilder();

                sb.Append("<div style='font-family:system-ui,-apple-system,Segoe UI,Roboto,Arial;line-height:1.4'>");
                sb.Append($"<h2 style='margin:0 0 10px'>Nuevo pedido #{order.Id}</h2>");
                sb.Append("<div style='color:#555;margin-bottom:14px'>");
                sb.Append($"<div><b>Fecha:</b> {order.CreatedAt:yyyy-MM-dd HH:mm} (UTC)</div>");
                sb.Append($"<div><b>Cliente:</b> {System.Net.WebUtility.HtmlEncode(order.FullName)}</div>");
                sb.Append($"<div><b>Teléfono:</b> {System.Net.WebUtility.HtmlEncode(order.Phone)}</div>");
                if (!string.IsNullOrWhiteSpace(order.Email)) sb.Append($"<div><b>Email:</b> {System.Net.WebUtility.HtmlEncode(order.Email)}</div>");
                sb.Append($"<div><b>Método:</b> {System.Net.WebUtility.HtmlEncode(order.PaymentMethod)}</div>");
                sb.Append($"<div><b>Total estimado:</b> {Money(order.TotalCrc)}</div>");
                sb.Append($"<div><b>Estado:</b> {System.Net.WebUtility.HtmlEncode(order.Status)}</div>");
                sb.Append("</div>");

                sb.Append("<div style='border:1px solid #eee;border-radius:12px;overflow:hidden'>");
                sb.Append("<table style='width:100%;border-collapse:collapse'>");
                sb.Append("<thead><tr style='background:#fafafa'>");
                sb.Append("<th style='text-align:left;padding:10px;border-bottom:1px solid #eee'>Producto</th>");
                sb.Append("<th style='text-align:left;padding:10px;border-bottom:1px solid #eee'>Opción</th>");
                sb.Append("<th style='text-align:right;padding:10px;border-bottom:1px solid #eee'>Cantidad</th>");
                sb.Append("<th style='text-align:right;padding:10px;border-bottom:1px solid #eee'>Precio</th>");
                sb.Append("<th style='text-align:right;padding:10px;border-bottom:1px solid #eee'>Subtotal</th>");
                sb.Append("</tr></thead><tbody>");

                foreach (var it in items)
                {
                    sb.Append("<tr>");
                    sb.Append($"<td style='padding:10px;border-bottom:1px solid #f1f1f1'>{System.Net.WebUtility.HtmlEncode(it.ProductName)}</td>");
                    sb.Append($"<td style='padding:10px;border-bottom:1px solid #f1f1f1'>{System.Net.WebUtility.HtmlEncode(it.Option ?? string.Empty)}</td>");
                    sb.Append($"<td style='padding:10px;border-bottom:1px solid #f1f1f1;text-align:right'>{it.Quantity}</td>");
                    sb.Append($"<td style='padding:10px;border-bottom:1px solid #f1f1f1;text-align:right'>{Money(it.PriceCrc)}</td>");
                    sb.Append($"<td style='padding:10px;border-bottom:1px solid #f1f1f1;text-align:right'>{Money(it.SubtotalCrc)}</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</tbody></table></div>");

                if (!string.IsNullOrWhiteSpace(order.Address))
                {
                    sb.Append("<div style='margin-top:14px'>");
                    sb.Append($"<div><b>Dirección:</b> {System.Net.WebUtility.HtmlEncode(order.Address)}</div>");
                    sb.Append("</div>");
                }

                if (!string.IsNullOrWhiteSpace(order.Notes))
                {
                    sb.Append("<div style='margin-top:10px'>");
                    sb.Append($"<div><b>Notas:</b> {System.Net.WebUtility.HtmlEncode(order.Notes)}</div>");
                    sb.Append("</div>");
                }

                sb.Append("<div style='margin-top:16px;color:#777;font-size:12px'>Saint Hub · Alerta automática</div>");
                sb.Append("</div>");

                await _email.SendAsync(_opt.Recipients, subject, sb.ToString());
            }
            catch
            {
                // NUNCA tumba el checkout por email.
                return;
            }
        }
    }
}
