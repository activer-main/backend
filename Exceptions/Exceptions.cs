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
    public UnauthorizedException(string message) : base(message)
    {

    }
}

public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message)
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
