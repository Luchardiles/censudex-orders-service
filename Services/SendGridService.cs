using SendGrid;
using SendGrid.Helpers.Mail;

namespace censudex_orders.Services
{
    /// <summary>
    /// Interfaz para el servicio de notificaciones por email
    /// </summary>
    public interface IEmailService
    {
        Task SendOrderCreatedEmailAsync(string toEmail, string clientName, Guid orderId, decimal totalAmount);
        Task SendOrderProcessingEmailAsync(string toEmail, string clientName, Guid orderId);
        Task SendOrderShippedEmailAsync(string toEmail, string clientName, Guid orderId, string trackingNumber);
        Task SendOrderDeliveredEmailAsync(string toEmail, string clientName, Guid orderId);
        Task SendOrderCancelledEmailAsync(string toEmail, string clientName, Guid orderId, string reason);
    }

    /// <summary>
    /// Servicio para enviar notificaciones por correo electrónico usando SendGrid
    /// Se activa en cada cambio de estado del pedido
    /// </summary>
    public class SendGridService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SendGridService> _logger;
        private readonly SendGridClient _client;

        public SendGridService(IConfiguration configuration, ILogger<SendGridService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var apiKey = _configuration["SendGrid:ApiKey"];
            _client = new SendGridClient(apiKey);
        }

        /// <summary>
        /// Envía email de confirmación cuando se crea un pedido
        /// </summary>
        public async Task SendOrderCreatedEmailAsync(string toEmail, string clientName, Guid orderId, decimal totalAmount)
        {
            try
            {
                // Email por defecto desde appsettings.json
                var defaultEmail = _configuration["SendGrid:DefaultRecipient"];

                // Si toEmail es nulo o vacío, usar el default
                var finalEmail = string.IsNullOrWhiteSpace(toEmail) ? defaultEmail : toEmail;

                var from = new EmailAddress(
                    _configuration["SendGrid:FromEmail"],
                    _configuration["SendGrid:FromName"]
                );

                var to = new EmailAddress(finalEmail, clientName);
                var subject = $"Confirmación de Pedido #{orderId.ToString()[..8]}";

                var htmlContent = $@"
                    <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <h2>¡Gracias por tu compra, {clientName}!</h2>
                            <p>Tu pedido ha sido recibido exitosamente.</p>
                            <div style='background-color: #f4f4f4; padding: 15px; border-radius: 5px;'>
                                <p><strong>Número de Pedido:</strong> {orderId}</p>
                                <p><strong>Total:</strong> ${totalAmount:N2}</p>
                                <p><strong>Fecha:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
                            </div>
                            <p>Recibirás actualizaciones sobre el estado de tu pedido.</p>
                            <p>Saludos,<br>Equipo Censudex</p>
                        </body>
                    </html>
                ";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                var response = await _client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Email enviado exitosamente a {finalEmail}");
                }
                else
                {
                    _logger.LogWarning($"Error al enviar email: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al enviar email de pedido creado a {toEmail}");
            }
        }


        /// <summary>
        /// Envía email cuando el pedido está en procesamiento
        /// </summary>
        public async Task SendOrderProcessingEmailAsync(string toEmail, string clientName, Guid orderId)
        {
            try
            {
                var from = new EmailAddress(
                    _configuration["SendGrid:FromEmail"],
                    _configuration["SendGrid:FromName"]
                );
                var to = new EmailAddress(toEmail, clientName);
                var subject = $"Tu pedido está siendo procesado #{orderId.ToString()[..8]}";

                var htmlContent = $@"
                    <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <h2>Hola {clientName},</h2>
                            <p>Tu pedido <strong>#{orderId.ToString()[..8]}</strong> está siendo preparado.</p>
                            <p>Estamos empacando tus productos con mucho cuidado.</p>
                            <p>Te notificaremos cuando sea enviado.</p>
                            <p>Saludos,<br>Equipo Censudex</p>
                        </body>
                    </html>
                ";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                await _client.SendEmailAsync(msg);

                _logger.LogInformation($"Email de pedido en procesamiento enviado a {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al enviar email de procesamiento a {toEmail}");
            }
        }

        /// <summary>
        /// Envía email cuando el pedido es enviado
        /// </summary>
        public async Task SendOrderShippedEmailAsync(string toEmail, string clientName, Guid orderId, string trackingNumber)
        {
            try
            {
                var from = new EmailAddress(
                    _configuration["SendGrid:FromEmail"],
                    _configuration["SendGrid:FromName"]
                );
                var to = new EmailAddress(toEmail, clientName);
                var subject = $"Tu pedido ha sido enviado #{orderId.ToString()[..8]}";

                var htmlContent = $@"
                    <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <h2>¡Tu pedido está en camino, {clientName}!</h2>
                            <p>Tu pedido <strong>#{orderId.ToString()[..8]}</strong> ha sido enviado.</p>
                            <div style='background-color: #e8f5e9; padding: 15px; border-radius: 5px;'>
                                <p><strong>Número de seguimiento:</strong> {trackingNumber}</p>
                                <p>Puedes rastrear tu envío con este número.</p>
                            </div>
                            <p>Tiempo estimado de entrega: 3-5 días hábiles</p>
                            <p>Saludos,<br>Equipo Censudex</p>
                        </body>
                    </html>
                ";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                await _client.SendEmailAsync(msg);

                _logger.LogInformation($"Email de pedido enviado a {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al enviar email de envío a {toEmail}");
            }
        }

        /// <summary>
        /// Envía email cuando el pedido es entregado
        /// </summary>
        public async Task SendOrderDeliveredEmailAsync(string toEmail, string clientName, Guid orderId)
        {
            try
            {
                var from = new EmailAddress(
                    _configuration["SendGrid:FromEmail"],
                    _configuration["SendGrid:FromName"]
                );
                var to = new EmailAddress(toEmail, clientName);
                var subject = $"Tu pedido ha sido entregado #{orderId.ToString()[..8]}";

                var htmlContent = $@"
                    <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <h2>¡Pedido entregado, {clientName}!</h2>
                            <p>Tu pedido <strong>#{orderId.ToString()[..8]}</strong> ha sido entregado exitosamente.</p>
                            <p>Esperamos que disfrutes tus productos.</p>
                            <p>Si tienes algún problema, no dudes en contactarnos.</p>
                            <p>¡Gracias por comprar en Censudex!</p>
                            <p>Saludos,<br>Equipo Censudex</p>
                        </body>
                    </html>
                ";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                await _client.SendEmailAsync(msg);

                _logger.LogInformation($"Email de pedido entregado enviado a {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al enviar email de entrega a {toEmail}");
            }
        }

        /// <summary>
        /// Envía email cuando el pedido es cancelado
        /// </summary>
        public async Task SendOrderCancelledEmailAsync(string toEmail, string clientName, Guid orderId, string reason)
        {
            try
            {
                var from = new EmailAddress(
                    _configuration["SendGrid:FromEmail"],
                    _configuration["SendGrid:FromName"]
                );
                var to = new EmailAddress(toEmail, clientName);
                var subject = $"Pedido cancelado #{orderId.ToString()[..8]}";

                var htmlContent = $@"
                    <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <h2>Pedido cancelado</h2>
                            <p>Hola {clientName},</p>
                            <p>Tu pedido <strong>#{orderId.ToString()[..8]}</strong> ha sido cancelado.</p>
                            <div style='background-color: #ffebee; padding: 15px; border-radius: 5px;'>
                                <p><strong>Motivo:</strong> {reason}</p>
                            </div>
                            <p>Si el pago fue procesado, el reembolso se realizará en los próximos 5-7 días hábiles.</p>
                            <p>Si tienes alguna duda, contáctanos.</p>
                            <p>Saludos,<br>Equipo Censudex</p>
                        </body>
                    </html>
                ";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                await _client.SendEmailAsync(msg);

                _logger.LogInformation($"Email de pedido cancelado enviado a {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al enviar email de cancelación a {toEmail}");
            }
        }
    }
}