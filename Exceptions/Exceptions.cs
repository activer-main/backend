namespace ActiverWebAPI.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {

    }

    public NotFoundException(string message, Exception innerException) : base(message , innerException)
    {

    }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException() : base()
    {

    }

    public UnauthorizedException(string message) : base(message)
    {

    }

    public UnauthorizedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message)
    {

    }

    public BadRequestException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class UserNotFoundException : NotFoundException
{
    public UserNotFoundException() : base("使用者不存在")
    {
    }

    public UserNotFoundException(string message) : base(message)
    {
    }

    public UserNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class TagNotFoundException : NotFoundException
{
    public TagNotFoundException(int tagId) : base($"Tag Id: {tagId} 不存在")
    {

    }
    public TagNotFoundException(int tagId, Exception innerException) : base($"Tag Id: {tagId} 不存在", innerException)
    {

    }
}