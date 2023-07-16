using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace AutoKkutuGui.Config;

/// <summary>
/// <para>Author: Logix@stackoverflow, Wiesław Šoltés@stackoverflow,and dnr3@stackoverflow</para>
/// <para>https://stackoverflow.com/a/63442626</para>
/// </summary>
/// <typeparam name="TItem">The item type to be stored in this list</typeparam>
public class ReorderableList<TItem> where TItem : class
{
	private readonly string DisplayMemberPath;

	// The name of the member in to display
	private readonly IList<TItem> ItemList = new ObservableCollection<TItem>();

	private readonly ListBox ItemListBox; // The target ListBox we're modifying
	private readonly Button MoveUpButton;
	private readonly Button MoveDownButton;

	private Point CursorStartPosition;

	public System.Collections.IList SelectedItems => ItemListBox.SelectedItems;

	/// <summary>
	/// Initializes the list (this must be done after components are initialized and loaded!).
	/// </summary>
	/// <param name="listBox">The target ListBox control to modify</param>
	/// <param name="displayMemberPath">The name of the member in the generic type contained in this list, to be displayed</param>
	public ReorderableList(ListBox listBox, Button moveUpButton, Button moveDownButton, string displayMemberPath)
	{
		ItemListBox = listBox;
		MoveUpButton = moveUpButton;
		MoveDownButton = moveDownButton;
		DisplayMemberPath = displayMemberPath;

		Initialize();
	}

	/// <summary>
	/// Adds an item to the list. If [ignoreDuplicates] is false and the item is already in the list,
	/// the item won't be added.
	/// </summary>
	/// <param name="item">The item to add</param>
	/// <param name="ignoreDuplicates">Whether or not to add the item regardless of whether it's already in the list</param>
	/// <returns>Whether or not the item was added</returns>
	public bool Add(TItem item, bool ignoreDuplicates = true)
	{
		if (!ignoreDuplicates && Contains(item))
			return false;

		ItemList.Add(item);
		return true;
	}

	/// <summary>
	/// Returns whether or not the list contains the given item.
	/// </summary>
	/// <param name="item">The item to check for</param>
	/// <returns>Whether or not the list contains the given item.</returns>
	public bool Contains(TItem item) => ItemList.Contains(item);

	/// <summary>
	/// Removes an item from the list.
	/// </summary>
	/// <param name="item">The item to remove</param>
	/// <returns>Whether or not the item was removed from the list. This will be false if the item was not in the list to begin with.</returns>
	public bool Remove(TItem item)
	{
		if (!Contains(item))
			return false;

		ItemList.Remove(item);
		return true;
	}

	public TItem[] ToArray() => ItemList.ToArray();

	private T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
	{
		var parentObject = VisualTreeHelper.GetParent(child);
		if (parentObject == null)
			return null;
		return parentObject is T parent ? parent : FindVisualParent<T>(parentObject);
	}

	private void Initialize()
	{
		// Set the list box's items source and tell it what member in the IT class to use for the display name
		// Add an event handler for preview mouse move

		ItemListBox.DisplayMemberPath = DisplayMemberPath;
		ItemListBox.ItemsSource = ItemList;
		ItemListBox.PreviewMouseMove += OnListPreviewMouseMove;

		// Create the item container style to be used by the listbox
		// Add mouse event handlers to the style

		var style = new Style(typeof(ListBoxItem));
		style.Setters.Add(new Setter(UIElement.AllowDropProperty, true));
		style.Setters.Add(new EventSetter(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnListPreviewMouseLeftButtonDown)));
		style.Setters.Add(new EventSetter(UIElement.DropEvent, new DragEventHandler(OnListDrop)));

		// Set the item container style

		ItemListBox.ItemContainerStyle = style;

		ItemListBox.SelectionMode = SelectionMode.Extended;
		MoveUpButton.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnMoveUpClick));
		MoveDownButton.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnMoveDownClick));
	}

	private void OnMoveUpClick(object sender, RoutedEventArgs args)
	{
		var items = (from object itemObj in ItemListBox.SelectedItems where itemObj is TItem let item = itemObj as TItem select item).ToList();
		var itemCount = items.Count;
		if (itemCount == 0)
			return;
		for (var i = 0; i < itemCount; i++)
		{
			var selectedItem = items[i];
			var oldIndex = ItemListBox.Items.IndexOf(selectedItem);
			var newIndex = oldIndex == 0 ? itemCount - 1 : oldIndex - 1;

			ItemList.Remove(selectedItem);
			ItemList.Insert(newIndex, selectedItem);
		}
	}

	// https://stackoverflow.com/a/31598801
	private void OnMoveDownClick(object sender, RoutedEventArgs args)
	{
		var items = (from object itemObj in ItemListBox.SelectedItems where itemObj is TItem let item = itemObj as TItem select item).ToList();
		var itemCount = items.Count;
		if (itemCount == 0)
			return;
		for (var i = itemCount - 1; i >= 0; i--)
		{
			var selectedItem = items[i];
			var oldIndex = ItemListBox.Items.IndexOf(selectedItem);
			var newIndex = oldIndex == itemCount - 1 ? 0 : oldIndex + 1;

			ItemList.Remove(selectedItem);
			ItemList.Insert(newIndex, selectedItem);
		}
	}

	private void Move(TItem source, int sourceIndex, int targetIndex)
	{
		if (sourceIndex < targetIndex)
		{
			ItemList.Insert(targetIndex + 1, source);
			ItemList.RemoveAt(sourceIndex);
		}
		else
		{
			var removeIndex = sourceIndex + 1;
			if (ItemList.Count + 1 > removeIndex)
			{
				ItemList.Insert(targetIndex, source);
				ItemList.RemoveAt(removeIndex);
			}
		}
	}

	private void OnListDrop(object sender, DragEventArgs e)
	{
		if (sender is ListBoxItem item)
		{
			var source = (TItem)e.Data.GetData(typeof(TItem));
			var target = item.DataContext as TItem;

			var sourceIndex = ItemListBox.Items.IndexOf(source);
			var targetIndex = ItemListBox.Items.IndexOf(target);

			Move(source, sourceIndex, targetIndex);
		}
	}

	private void OnListPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => CursorStartPosition = e.GetPosition(null);

	private void OnListPreviewMouseMove(object sender, MouseEventArgs e)
	{
		var currentCursorPos = e.GetPosition(null);
		var cursorVector = CursorStartPosition - currentCursorPos;

		if (e.LeftButton == MouseButtonState.Pressed
			&& (Math.Abs(cursorVector.X) > SystemParameters.MinimumHorizontalDragDistance
			|| Math.Abs(cursorVector.Y) > SystemParameters.MinimumVerticalDragDistance))
		{
			var targetItem = FindVisualParent<ListBoxItem>((DependencyObject)e.OriginalSource);
			if (targetItem != null)
				DragDrop.DoDragDrop(targetItem, targetItem.DataContext, DragDropEffects.Move);
		}
	}
}
