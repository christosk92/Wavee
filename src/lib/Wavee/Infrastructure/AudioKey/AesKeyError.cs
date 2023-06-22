namespace Wavee.Infrastructure.AudioKey;

public class AesKeyError : Exception
{
    public AesKeyError(byte ErrorCode, byte ErrorType) : base($"Error code: {ErrorCode}, Error type: {ErrorType}")
    {
        this.ErrorCode = ErrorCode;
        this.ErrorType = ErrorType;
    }

    public byte ErrorCode { get; init; }
    public byte ErrorType { get; init; }

    public void Deconstruct(out byte ErrorCode, out byte ErrorType)
    {
        ErrorCode = this.ErrorCode;
        ErrorType = this.ErrorType;
    }
}