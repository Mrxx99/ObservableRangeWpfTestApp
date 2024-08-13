using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ObservableRange;

namespace ObservableRangeWpfTestApp;

public partial class MainWindowViewModel : ObservableObject
{
    private int _itemCounter = 0;
    private int _replaceCounter = 0;
    private static readonly object _lock = new();

    [ObservableProperty]
    private bool _enableCollectionSynchronization = false;

    public ObservableRangeCollection<string> ObservableItems { get; } = new();

    [RelayCommand]
    private void AddItem()
    {
        ObservableItems.Add($"Item {_itemCounter++}");
    }

    [RelayCommand]
    private void AddItemsRange()
    {
        ObservableItems.AddRange(Enumerable.Repeat(() => $"Item {_itemCounter++}", 18).Select(g => g()));
    }

    [RelayCommand]
    private void RemoveItem()
    {
        if (ObservableItems.Count < 1)
        {
            return;
        }
        ObservableItems.RemoveAt(0);
    }

    [RelayCommand]
    private void RemoveItemsRange()
    {
        if (ObservableItems.Count < 1)
        {
            return;
        }
        else if (ObservableItems.Count == 1)
        {
            ObservableItems.RemoveRange(0, 1);
        }
        else if (ObservableItems.Count == 2 || ObservableItems.Count == 3)
        {
            ObservableItems.RemoveRange(0, 2);
        }
        else if (ObservableItems.Count < 20)
        {
            ObservableItems.RemoveRange(1, 2 + (ObservableItems.Count > 4 ? 1 : 0));
        }
        else
        {
            ObservableItems.RemoveRange(14, 4); // over the block size border of ItemBlocks in ItemContainerGenerator
        }
    }

    [RelayCommand]
    private void ReplaceItem()
    {
        _replaceCounter++;
        if (ObservableItems.Count < 1)
        {
            return;
        }
        ObservableItems.Replace(0, ObservableItems[0] + "_" + _replaceCounter);
    }

    [RelayCommand]
    private void ReplaceItemsRange()
    {
        _replaceCounter++;
        if (ObservableItems.Count < 1)
        {
            return;
        }
        else if (ObservableItems.Count == 1)
        {
            ObservableItems.ReplaceRange(0, 1, [ObservableItems[0] + "_" + _replaceCounter]);
        }
        else if (ObservableItems.Count == 2 || ObservableItems.Count == 3)
        {
            var newItems = ObservableItems.Take(2).Select(i => i + "_" + _replaceCounter).Concat(["Item_" + _replaceCounter, "Item_" + _replaceCounter + 1]).ToArray();
            ObservableItems.ReplaceRange(0, 2, newItems);
        }
        else if (ObservableItems.Count < 20)
        {
            // more removed than new
            var newItems = ObservableItems.Skip(1).Take(2 + (ObservableItems.Count > 4 ? 1 : 0)).Select(i => i + "_" + _replaceCounter).ToArray();
            ObservableItems.ReplaceRange(1, 5, newItems);
        }
        else
        {
            // more new than removed
            var newItems = ObservableItems.Skip(14).Take(5).Select(i => i + "_" + _replaceCounter).ToArray();
            ObservableItems.ReplaceRange(14, 3, newItems);
        }
    }

    partial void OnEnableCollectionSynchronizationChanged(bool value)
    {
        if (value)
        {
            BindingOperations.EnableCollectionSynchronization(ObservableItems, _lock);
        }
        else
        {
            BindingOperations.DisableCollectionSynchronization(ObservableItems);
        }
    }
}
