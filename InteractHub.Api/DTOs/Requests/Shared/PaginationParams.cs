namespace InteractHub.Api.DTOs.Requests;

public abstract class PaginationParams
{
    public int Page { get; set; } = 1; 
    
    private int _limit = 10;
    protected const int MaxLimit = 50; 

    public int PageSize
    {
        get => _limit;
        set => _limit = (value > MaxLimit) ? MaxLimit : value;
    }
}