namespace StudyOrganizer.Application.Authentication;

public interface IAccessTokenService
{
    AccessTokenResult Create(
        Guid userId,
        string email);
}
