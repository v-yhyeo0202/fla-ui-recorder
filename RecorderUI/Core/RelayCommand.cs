using System.Windows.Input;

namespace RecorderUI.Core;

public class RelayCommand : ICommand
{
    private Action<object> execute;
    public event EventHandler? CanExecuteChanged;

    public RelayCommand(Action<object> _execute)
    {
        execute = _execute;

        return;
    }

    public bool CanExecute(object? param)
    {
        return true;
    }

    public void Execute(object? param)
    {
        execute(param);

        return;
    }
}

public class RelayCommandAsync : ICommand
{
    private Func<object, Task> execute;
    private bool bExecute = false;
    public event EventHandler CanExecuteChanged;

    public RelayCommandAsync(Func<object, Task> _execute)
    {
        execute = _execute;

        return;
    }

    public bool CanExecute(object param)
    {
        return !bExecute;
    }

    public async void Execute(object param)
    {
        if (CanExecute(param))
        {
            bExecute = true;
            await execute(param);
            bExecute = false;
        }

        return;
    }
}