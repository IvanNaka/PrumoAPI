namespace Prumo.Application.Interfaces
{
    public interface IAuthService
    {
        Task<string> SignInWithGoogleAsync(string idToken);
    }
}