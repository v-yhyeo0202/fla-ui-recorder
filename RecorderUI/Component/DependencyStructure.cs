namespace RecorderUI.Component;

public interface IFormDependency
{
    void SetComboBoxItem(object _listItem, string propertyName);
    Task SetComboBoxItemAsync(object _listItem, string propertyName);
    Task PostInsertAsync(object _listInput);
    void PostApply(object _listInput);
}