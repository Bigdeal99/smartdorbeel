using System.ComponentModel.DataAnnotations;
using lib;

namespace Api.Dtos;

public class ServerSendsErrorMessageDto : BaseDto
{
    public string ErrorMessage { get; set; }
}

public class ClientWantsToSeeStreamDto : BaseDto
{
    public string Topic { get; set; }
    public string Command { get; set; }
}

public class ClientWantsToSignInWithNameDto : BaseDto
{
    [Required(ErrorMessage = "Username is required")]
    [MinLength(2, ErrorMessage = "Username must be at least 2 characters long")]
    [MaxLength(25, ErrorMessage = "Username must not be longer than 25 characters")]
    public string Name { get; set; }
}

public class ClientWantsToGetBellLogDto : BaseDto
{
    
}

public class ServerSendsInfoToClient : BaseDto
{
    public string Message { get; set; }
}

public class ClientWantsToDeleteSingleLogDto : BaseDto
{
    public string FileName { get; set; }
}

public class ClientWantsToSearchForImagesDto : BaseDto
{
    public string DateTime { get; set; }
}