using System;
using System.Collections.Generic;
using System.Text;

namespace ERDC.Application.Interfaces
{
    public interface ICotizacionService
    {
        Task<byte[]> GenerarCotizacionPdf(int idPedido);
    }
}
