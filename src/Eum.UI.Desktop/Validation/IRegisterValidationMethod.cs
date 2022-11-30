namespace Eum.UI.Validation;

public interface IRegisterValidationMethod
{
    void RegisterValidationMethod(string propertyName, ValidateMethod validateMethod);
}