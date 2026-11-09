using Control.Api.Application.Validation;

namespace Control.Api.Application.Interfaces;

public interface IExcelFileValidator
{
    FileValidationResult Validate(string fileName, long length, Stream content);
}
