public class Pagination<T> : BaseViewModel
{
    private int _pageSize = 6;
    private int _currentPage = 1;

    public ObservableCollection<T> SourceItems { get; } = new();
    public ObservableCollection<T> PagedItems { get; } = new();

    public bool HasItems => SourceItems.Count > 0;
    public bool NoItems => !HasItems;

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (SetProperty(ref _pageSize, value))
            {
                CurrentPage = 1;
                Update();
            }
        }
    }

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
                Update();
        }
    }

    public int TotalPages =>
        SourceItems.Count == 0
            ? 1
            : (int)Math.Ceiling((double)SourceItems.Count / PageSize);

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public string PageInfo => $"الصفحة {CurrentPage} من {TotalPages}";

    public ICommand PrevPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand FirstPageCommand { get; }
    public ICommand LastPageCommand { get; }

    public Pagination()
    {
        PrevPageCommand = new RelayCommand(() => CurrentPage--, () => HasPreviousPage);
        NextPageCommand = new RelayCommand(() => CurrentPage++, () => HasNextPage);
        FirstPageCommand = new RelayCommand(() => CurrentPage = 1, () => HasPreviousPage);
        LastPageCommand = new RelayCommand(() => CurrentPage = TotalPages, () => HasNextPage);
    }

    public void SetItems(IEnumerable<T> items)
    {
        SourceItems.Clear();
        foreach (var item in items)
            SourceItems.Add(item);

        CurrentPage = 1;
        Update();

        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(NoItems));
    }
    
    public IEnumerable<int> PageSizeOptions { get; } = new[] { 3, 6, 9, 12 };
    private void Update()
    {
        PagedItems.Clear();

        foreach (var item in SourceItems
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize))
        {
            PagedItems.Add(item);
        }

        OnPropertyChanged(nameof(PageInfo));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(HasPreviousPage));
        OnPropertyChanged(nameof(HasNextPage));

        CommandManager.InvalidateRequerySuggested();
    }
}
