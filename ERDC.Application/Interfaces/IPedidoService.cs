using System;
using System.Collections.Generic;
using System.Text;

namespace ERDC.Application.Interfaces
{
    using ERDC.Application.DTOs;
    using ERDC.Domain.Entities;

    // ====================================================================
    // Interfaces de la Capa 2 (Sección 2.1.3 Diagrama de Clases y 2.1.4 Secuencia)
    // Ubicación: ERDC.Application/Interfaces/IServices.cs
    // ====================================================================
    public interface IPedidoService
    {
        Task<bool> VerificarDisponibilidad(List<ItemCarritoDto> items);
        Task DescontarStock(List<ItemCarritoDto> items);
        Task<Pedido> RegistrarPedidoOCotizacionAsync(CrearPedidoDto dto);
    }

}
