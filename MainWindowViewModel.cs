using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Data;
using Bogus;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ObservableRange;

namespace ObservableRangeWpfTestApp;

public partial class Person : ObservableObject
{
    public static int IdCounter { get; set; }
    public static int ReplaceCounter { get; set; }

    [ObservableProperty]
    private string _name = default!;

    [ObservableProperty]
    [property: JsonIgnore]
    private int _age;

    public int Id { get; set; }

    [JsonIgnore]
    public int AgeDecade => (Age / 10) * 10;

    [JsonIgnore]
    public char FirstLetter => Name[0];

    public override string ToString() => $"#{Id} {Name} ({Age})";

    public Person CreateReplaced()
    {
        return new Person
        {
            Id = Id,
            Name = $"{(char)Random.Shared.Next('a', 'z')}{Name}_{++ReplaceCounter}",
            Age = Age
        };
    }

    public static IReadOnlyList<Person> Create(int count)
    {
        var faker = new Faker<Person>()
            .RuleFor(p => p.Name, f => f.Name.FirstName())
            .RuleFor(p => p.Age, f => f.Random.Int(1, 36))
            .RuleFor(p => p.Id, f => IdCounter++);

        var people = faker.Generate(count);
        return people;
    }

    public static Person Create()
    {
        return Create(1)[0];
    }
}

public partial class MainWindowViewModel : ObservableObject
{
    private static readonly object _lock = new();
    private JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    [ObservableProperty]
    private bool _enableCollectionSynchronization = false;

    [ObservableProperty]
    private bool _enableSorting = false;

    [ObservableProperty]
    private bool _enableFiltering = false;

    [ObservableProperty]
    private bool _enableGrouping = false;

    [ObservableProperty]
    private bool _bindToCollectionView = false;

    [ObservableProperty]
    private bool _enableLiveFiltering = false;

    [ObservableProperty]
    private bool _enableLiveSorting = false;

    [ObservableProperty]
    private bool _enableLiveGrouping = false;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsItemSelected))]
    private Person? _selectedItem;

    public bool IsItemSelected => SelectedItem != null;

    public ObservableRangeCollection<Person> ObservableItems { get; } = new();
    private readonly CollectionViewSource _collectionViewSource = new();
    private ICollectionView _collectionView;

    [ObservableProperty]
    private IEnumerable _bindingSource;

    public ObservableCollection<string> Log { get; } = [];

    public MainWindowViewModel()
    {
        _collectionView = CollectionViewSource.GetDefaultView(ObservableItems);
        _collectionViewSource.Source = ObservableItems;
        BindingSource = ObservableItems;
        ObservableItems.CollectionChanged += ObservableItems_CollectionChanged;
    }

    private void ObservableItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        Log.Insert(0, JsonSerializer.Serialize(e, _serializerOptions));
    }

    [RelayCommand]
    private void AddItem()
    {
        ObservableItems.Add(Person.Create());
    }

    [RelayCommand]
    private void AddItemsRange()
    {
        ObservableItems.AddRange(Person.Create(18));
    }

    [RelayCommand]
    private void RemoveItem()
    {
        if (ObservableItems.Count < 1)
        {
            return;
        }

        if (SelectedItem != null)
        {
            ObservableItems.Remove(SelectedItem);
        }
        else
        {
            ObservableItems.RemoveAt(0);
        }
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
        if (ObservableItems.Count < 1)
        {
            return;
        }

        if (SelectedItem != null)
        {
            int replaceIndex = ObservableItems.IndexOf(SelectedItem);
            var newItem = SelectedItem.CreateReplaced();
            ObservableItems.Replace(replaceIndex, newItem);
            SelectedItem = newItem;
        }
    }

    [RelayCommand]
    private void ReplaceItemsRange()
    {
        if (ObservableItems.Count < 1)
        {
            return;
        }
        else if (ObservableItems.Count == 1)
        {
            ObservableItems.ReplaceRange(0, 1, [ObservableItems[0].CreateReplaced()]);
        }
        else if (ObservableItems.Count == 2 || ObservableItems.Count == 3)
        {
            var newItems = ObservableItems.Take(2).Select(i => i.CreateReplaced())
                .Concat([ObservableItems[1].CreateReplaced(), ObservableItems[1].CreateReplaced()]).ToArray();
            ObservableItems.ReplaceRange(0, 2, newItems);
        }
        else if (ObservableItems.Count == 4)
        {
            var newItems = ObservableItems.Take(2).Select(i => i.CreateReplaced()).ToArray();
            ObservableItems.ReplaceRange(0, 4, newItems);
        }
        else if (ObservableItems.Count < 20)
        {
            // more removed than new
            var newItems = ObservableItems.Skip(1).Take(3).Select(i => i.CreateReplaced()).ToArray();
            ObservableItems.ReplaceRange(1, 5, newItems);
        }
        else
        {
            // more new than removed
            var newItems = ObservableItems.Skip(14).Take(5).Select(i => i.CreateReplaced()).ToArray();
            ObservableItems.ReplaceRange(14, 3, newItems);
        }
    }

    [RelayCommand]
    private void ClearItems()
    {
        ObservableItems.Clear();
        Person.ReplaceCounter = 0;
        Person.IdCounter = 0;
    }

    partial void OnEnableCollectionSynchronizationChanged(bool value)
    {
        if (value)
        {
            BindingOperations.EnableCollectionSynchronization(BindingSource, _lock);
        }
        else
        {
            BindingOperations.DisableCollectionSynchronization(BindingSource);
        }
    }

    partial void OnEnableSortingChanged(bool value)
    {
        if (value)
        {
            _collectionView.SortDescriptions.Add(new SortDescription(nameof(Person.Name), ListSortDirection.Ascending));
        }
        else
        {
            _collectionView.SortDescriptions.Clear();
            EnableLiveGrouping = false;
        }
    }

    partial void OnEnableFilteringChanged(bool value)
    {
        if (value)
        {
            _collectionView.Filter = item => item is Person { Age: > 18 };
        }
        else
        {
            _collectionView.Filter = null;
            EnableLiveFiltering = false;
        }
    }

    partial void OnEnableGroupingChanged(bool value)
    {
        if (value)
        {
            if (!_collectionView.GroupDescriptions.Any())
            {
                _collectionView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Person.FirstLetter)));
            }
        }
        else
        {
            _collectionView.GroupDescriptions.Clear();
            EnableLiveGrouping = false;
        }
    }

    partial void OnBindToCollectionViewChanged(bool value)
    {
        if (value)
        {
            _collectionView = _collectionViewSource.View;
            BindingSource = _collectionView;
        }
        else
        {
            BindingSource = ObservableItems;
            _collectionView = CollectionViewSource.GetDefaultView(ObservableItems);
            EnableLiveSorting = false;
            EnableLiveFiltering = false;
            EnableLiveGrouping = false;
        }
        OnEnableCollectionSynchronizationChanged(EnableCollectionSynchronization);
        OnEnableFilteringChanged(EnableFiltering);
        OnEnableGroupingChanged(EnableGrouping);
        OnEnableSortingChanged(EnableSorting);
    }

    partial void OnEnableLiveFilteringChanged(bool value)
    {
        if (value)
        {
            _collectionViewSource.IsLiveFilteringRequested = true;
            _collectionViewSource.LiveFilteringProperties.Add(nameof(Person.Age));
            EnableFiltering = true;
        }
        else
        {
            _collectionViewSource.IsLiveFilteringRequested = false;
            _collectionViewSource.LiveFilteringProperties.Clear();
        }

        OnEnableFilteringChanged(EnableFiltering);
    }

    partial void OnEnableLiveSortingChanged(bool value)
    {
        if (value)
        {
            _collectionViewSource.IsLiveSortingRequested = true;
            _collectionViewSource.LiveSortingProperties.Add(nameof(Person.Name));
            EnableSorting = true;
        }
        else
        {
            _collectionViewSource.IsLiveSortingRequested = false;
            _collectionViewSource.LiveSortingProperties.Clear();
        }

        OnEnableSortingChanged(EnableSorting);
    }

    partial void OnEnableLiveGroupingChanged(bool value)
    {
        if (value)
        {
            _collectionViewSource.IsLiveGroupingRequested = true;
            _collectionViewSource.LiveGroupingProperties.Add(nameof(Person.FirstLetter));
            EnableGrouping = true;
        }
        else
        {
            _collectionViewSource.IsLiveGroupingRequested = false;
            _collectionViewSource.LiveGroupingProperties.Clear();
        }

        OnEnableGroupingChanged(EnableGrouping);
    }
}
