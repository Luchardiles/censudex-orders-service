using FluentValidation;
using censudex_orders.Models.DTOs;
using censudex_orders.Models.Enums;

namespace censudex_orders.Validators
{
    /// <summary>
    /// Validador para la creación de pedidos
    /// Valida que todos los campos requeridos estén presentes y sean correctos
    /// </summary>
    public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
    {
        public CreateOrderDtoValidator()
        {
            // Validar ClientId
            RuleFor(x => x.ClientId)
                .NotEmpty()
                .WithMessage("El ID del cliente es requerido")
                .Must(BeValidGuid)
                .WithMessage("El ID del cliente debe ser un GUID válido");

            // Validar dirección de envío
            RuleFor(x => x.ShippingAddress)
                .NotEmpty()
                .WithMessage("La dirección de envío es requerida")
                .MaximumLength(500)
                .WithMessage("La dirección no puede exceder 500 caracteres")
                .MinimumLength(10)
                .WithMessage("La dirección debe tener al menos 10 caracteres");

            // Validar items del pedido
            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage("El pedido debe contener al menos un producto")
                .Must(items => items != null && items.Count > 0)
                .WithMessage("El pedido debe contener al menos un producto");

            // Validar cada item
            RuleForEach(x => x.Items)
                .SetValidator(new OrderItemDtoValidator());
        }

        private bool BeValidGuid(Guid guid)
        {
            return guid != Guid.Empty;
        }
    }

    /// <summary>
    /// Validador para items individuales del pedido
    /// </summary>
    public class OrderItemDtoValidator : AbstractValidator<OrderItemDto>
    {
        public OrderItemDtoValidator()
        {
            // Validar ProductId
            RuleFor(x => x.ProductId)
                .NotEmpty()
                .WithMessage("El ID del producto es requerido")
                .Must(BeValidGuid)
                .WithMessage("El ID del producto debe ser un GUID válido");

            // Validar cantidad
            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("La cantidad debe ser mayor a 0")
                .LessThanOrEqualTo(1000)
                .WithMessage("La cantidad no puede exceder 1000 unidades por producto");
        }

        private bool BeValidGuid(Guid guid)
        {
            return guid != Guid.Empty;
        }
    }

    /// <summary>
    /// Validador para actualización de estado de pedido
    /// </summary>
    public class UpdateOrderStatusDtoValidator : AbstractValidator<UpdateOrderStatusDto>
    {
        public UpdateOrderStatusDtoValidator()
        {
            // Validar estado
            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("El estado es requerido")
                .Must(BeValidStatus)
                .WithMessage("El estado debe ser uno de los siguientes: Pendiente, EnProcesamiento, Enviado, Entregado, Cancelado");

            // Validar tracking number solo si el estado es "Enviado"
            RuleFor(x => x.TrackingNumber)
                .NotEmpty()
                .When(x => x.Status == OrderStatus.Enviado.ToString())
                .WithMessage("El número de seguimiento es requerido cuando el estado es 'Enviado'")
                .MaximumLength(100)
                .WithMessage("El número de seguimiento no puede exceder 100 caracteres");
        }

        private bool BeValidStatus(string status)
        {
            return Enum.TryParse<OrderStatus>(status, out _);
        }
    }

    /// <summary>
    /// Validador para cancelación de pedido
    /// </summary>
    public class CancelOrderDtoValidator : AbstractValidator<CancelOrderDto>
    {
        public CancelOrderDtoValidator()
        {
            // Validar motivo de cancelación
            RuleFor(x => x.CancellationReason)
                .NotEmpty()
                .WithMessage("El motivo de cancelación es requerido")
                .MinimumLength(10)
                .WithMessage("El motivo debe tener al menos 10 caracteres")
                .MaximumLength(500)
                .WithMessage("El motivo no puede exceder 500 caracteres");
        }
    }

    /// <summary>
    /// Validador para filtros de búsqueda de pedidos
    /// </summary>
    public class OrderFilterDtoValidator : AbstractValidator<OrderFilterDto>
    {
        public OrderFilterDtoValidator()
        {
            // Validar OrderId si está presente
            RuleFor(x => x.OrderId)
                .Must(BeValidGuid)
                .When(x => x.OrderId.HasValue)
                .WithMessage("El ID del pedido debe ser un GUID válido");

            // Validar ClientId si está presente
            RuleFor(x => x.ClientId)
                .Must(BeValidGuid)
                .When(x => x.ClientId.HasValue)
                .WithMessage("El ID del cliente debe ser un GUID válido");

            // Validar rango de fechas
            RuleFor(x => x)
                .Must(HaveValidDateRange)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("La fecha de inicio debe ser anterior a la fecha de fin");

            // Validar que las fechas no sean futuras
            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(DateTime.Now)
                .When(x => x.StartDate.HasValue)
                .WithMessage("La fecha de inicio no puede ser futura");

            RuleFor(x => x.EndDate)
                .LessThanOrEqualTo(DateTime.Now)
                .When(x => x.EndDate.HasValue)
                .WithMessage("La fecha de fin no puede ser futura");
        }

        private bool BeValidGuid(Guid? guid)
        {
            return !guid.HasValue || guid.Value != Guid.Empty;
        }

        private bool HaveValidDateRange(OrderFilterDto filter)
        {
            if (!filter.StartDate.HasValue || !filter.EndDate.HasValue)
                return true;

            return filter.StartDate.Value <= filter.EndDate.Value;
        }
    }
}