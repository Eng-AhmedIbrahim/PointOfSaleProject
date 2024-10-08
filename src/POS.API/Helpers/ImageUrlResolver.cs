namespace POS.API.Helpers;

public class ImageUrlResolver : IValueResolver<Branch, BranchToReturnDto, string>
{
    private readonly IConfiguration _configuration;

    public ImageUrlResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Resolve(Branch source, BranchToReturnDto destination, string destMember, ResolutionContext context)
    {
        return $"{_configuration["ApiBaseUrl"]}/{source.Logo}";
    }
}