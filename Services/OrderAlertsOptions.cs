namespace SaintHub.Services
{
    public class OrderAlertsOptions
    {
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Lista de correos que reciben alerta cuando entra un pedido.
        /// </summary>
        public string[] Recipients { get; set; } = System.Array.Empty<string>();

        /// <summary>
        /// Prefijo del asunto (ej: "[Saint Hub]").
        /// </summary>
        public string SubjectPrefix { get; set; } = "[Saint Hub]";
    }
}
