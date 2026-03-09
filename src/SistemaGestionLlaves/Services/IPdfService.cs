using System.Collections.Generic;

namespace SistemaGestionLlaves.Services;

public interface IPdfService
{
    byte[] GenerateReportPdf<T>(string titulo, List<string> headers, List<T> data, List<float> columnWidths);
}
