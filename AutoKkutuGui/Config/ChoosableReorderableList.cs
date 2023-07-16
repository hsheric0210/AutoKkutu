using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace AutoKkutuGui.Config;

public record ChoosableReorderableListUIElements(ListBox InactiveItemListBox, ListBox ActiveItemListBox, Button ActivateItemButton, Button DeactivateItemButton, Button MoveUpItemButton, Button MoveDownItemButton);

/// <summary>
/// <para>Author: Logix@stackoverflow, Wiesław Šoltés@stackoverflow,and dnr3@stackoverflow</para>
/// <para>https://stackoverflow.com/a/63442626</para>
/// </summary>
/// <typeparam name="TItem">The item type to be stored in this list</typeparam>
public class ChoosableReorderableList<TItem> where TItem : class
{
	private readonly string DisplayMemberPath;

	private readonly IList<TItem> InactiveItemList = new ObservableCollection<TItem>();

	private readonly ListBox InactiveItemListBox;
	private readonly ReorderableList<TItem> ActiveItemReorderableList;

	private readonly Button ActivateItemButton;
	private readonly Button DeactivateItemButton;

	public ChoosableReorderableList(ChoosableReorderableListUIElements elements, string displayMemberPath)
	{
		if (elements == null)
			throw new ArgumentNullException(nameof(elements));

		DisplayMemberPath = displayMemberPath;
		InactiveItemListBox = elements.InactiveItemListBox;
		ActiveItemReorderableList = new ReorderableList<TItem>(elements.ActiveItemListBox, elements.MoveUpItemButton, elements.MoveDownItemButton, displayMemberPath);

		ActivateItemButton = elements.ActivateItemButton;
		DeactivateItemButton = elements.DeactivateItemButton;

		Initialize();
	}

	public void AddActive(TItem item) => ActiveItemReorderableList.Add(item);

	public void AddInactive(TItem item) => InactiveItemList.Add(item);

	public bool IsActive(TItem item) => ActiveItemReorderableList.Contains(item);

	public bool Deactivate(TItem item)
	{
		if (!IsActive(item))
			return false;

		ActiveItemReorderableList.Remove(item);
		InactiveItemList.Add(item);
		return true;
	}

	public bool Activate(TItem item)
	{
		if (IsActive(item))
			return false;

		ActiveItemReorderableList.Add(item);
		InactiveItemList.Remove(item);
		return true;
	}

	public TItem[] GetActiveItemArray() => ActiveItemReorderableList.ToArray();

	public TItem[] GetInactiveItemArray() => InactiveItemList.ToArray();

	private void Initialize()
	{
		InactiveItemListBox.DisplayMemberPath = DisplayMemberPath;
		InactiveItemListBox.ItemsSource = InactiveItemList;
		InactiveItemListBox.SelectionMode = SelectionMode.Extended;

		ActivateItemButton.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnActivateButtonClick));
		DeactivateItemButton.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnDeactivateButtonClick));
	}

	private void OnActivateButtonClick(object sender, RoutedEventArgs args)
	{
		foreach (var item in (from object itemObj in InactiveItemListBox.SelectedItems where itemObj is TItem let item = itemObj as TItem select item).ToArray())
		{
			InactiveItemList.Remove(item);
			ActiveItemReorderableList.Add(item);
		}
	}

	private void OnDeactivateButtonClick(object sender, RoutedEventArgs args)
	{
		foreach (var item in (from object itemObj in ActiveItemReorderableList.SelectedItems where itemObj is TItem let item = itemObj as TItem select item).ToArray())
		{
			ActiveItemReorderableList.Remove(item);
			InactiveItemList.Add(item);
		}
	}
}
