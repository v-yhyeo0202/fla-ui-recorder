namespace RecorderUI.ViewModel;

public class MainViewModel : BaseViewModel
{
    public Stack<BaseViewModel> stackViewModel;
    private BaseViewModel homeViewModel;
    private BaseViewModel _currentViewModel;
    public BaseViewModel currentViewModel { get { return _currentViewModel; } set { _currentViewModel = value; OnPropertyChanged(nameof(currentViewModel)); } }

    public MainViewModel(BaseViewModel entryViewModel)
    {
        stackViewModel = new Stack<BaseViewModel>();
        homeViewModel = entryViewModel;
        currentViewModel = entryViewModel;

        return;
    }

    public void Navigate(BaseViewModel viewModel, BaseViewModel storedViewModel = null)
    {
        if (storedViewModel != null)
        {
            stackViewModel.Push(storedViewModel);
        }

        currentViewModel = viewModel;

        return;
    }

    public void NavigateBack()
    {
        if (stackViewModel.Count > 0)
        {
            currentViewModel = stackViewModel.Pop();
        }
        else
        {
            currentViewModel = homeViewModel;
        }

        return;
    }
}