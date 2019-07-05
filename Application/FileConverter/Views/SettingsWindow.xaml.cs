﻿// <copyright file="SettingsWindow.xaml.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Views
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;

    using FileConverter.Diagnostics;
    using FileConverter.ViewModels;

    using GalaSoft.MvvmLight.Messaging;

    /// <summary>
    /// Interaction logic for Settings.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private enum DragDropTargetPosition
        {
            Before,
            After,
        }

        public SettingsWindow()
        {
            this.InitializeComponent();

            Messenger.Default.Register<string>(this, "DoFocus", this.DoFocus);
        }

        public void DoFocus(string message)
        {
            switch (message)
            {
                case "PresetName":
                    this.PresetNameTextBox.Focus();
                    this.PresetNameTextBox.SelectAll();
                    break;

                case "FolderName":
                    this.FolderNameTextBox.Focus();
                    this.FolderNameTextBox.SelectAll();
                    break;
            }
        }

        private void OnInputTypeChecked(object sender, RoutedEventArgs e)
        {
            SettingsViewModel dataContext = this.DataContext as SettingsViewModel;
            if (dataContext.SelectedPreset == null)
            {
                return;
            }

            CheckBox checkBox = sender as CheckBox;
            if (!checkBox.IsVisible)
            {
                return;
            }

            string inputFormat = (checkBox.Content as string).ToLowerInvariant();

            dataContext.SelectedPreset.Preset.AddInputType(inputFormat);
        }

        private void OnInputTypeUnchecked(object sender, RoutedEventArgs e)
        {
            SettingsViewModel dataContext = this.DataContext as SettingsViewModel;
            if (dataContext.SelectedPreset == null)
            {
                return;
            }

            CheckBox checkBox = sender as CheckBox;
            if (!checkBox.IsVisible)
            {
                return;
            }

            string inputFormat = (checkBox.Content as string).ToLowerInvariant();

            dataContext.SelectedPreset.Preset.RemoveInputType(inputFormat);
        }

        private void OnInputTypeCategoryChecked(object sender, RoutedEventArgs e)
        {
            SettingsViewModel dataContext = this.DataContext as SettingsViewModel;
            if (dataContext.SelectedPreset == null)
            {
                return;
            }

            CheckBox checkBox = sender as CheckBox;
            string categoryName = checkBox.Content as string;
            InputExtensionCategory category = dataContext.InputCategories.FirstOrDefault(match => match.Name == categoryName);
            if (category == null)
            {
                return;
            }

            foreach (string inputExtension in category.InputExtensionNames)
            {
                dataContext.SelectedPreset.Preset.AddInputType(inputExtension);
            }
        }

        private void OnInputTypeCategoryUnchecked(object sender, RoutedEventArgs e)
        {
            SettingsViewModel dataContext = this.DataContext as SettingsViewModel;
            if (dataContext.SelectedPreset == null)
            {
                return;
            }

            CheckBox checkBox = sender as CheckBox;
            string categoryName = checkBox.Content as string;
            InputExtensionCategory category = dataContext.InputCategories.FirstOrDefault(match => match.Name == categoryName);
            if (category == null)
            {
                return;
            }

            foreach (string inputExtension in category.InputExtensionNames)
            {
                dataContext.SelectedPreset.Preset.RemoveInputType(inputExtension);
            }
        }
        
        private void TreeView_MouseMove(object sender, MouseEventArgs args)
        {
            if (args.LeftButton == MouseButtonState.Pressed && 
                this.PresetTreeView.SelectedItem is AbstractTreeNode nodeToDrag)
            {
                var dataObj = new DataObject();
                dataObj.SetData("DragSource", nodeToDrag);

                DragDrop.DoDragDrop(this.PresetTreeView, dataObj, DragDropEffects.Move);
            }
        }

        private void TreeView_DragOver(object sender, DragEventArgs args)
        {
            args.Effects = DragDropEffects.None;
            args.Handled = true;

            if (args.Data.GetData("HighlightedItem") is TreeViewItem highlightedItem)
            {
                highlightedItem.BorderThickness = new Thickness(0);
            }

            if (this.TryComputeDropTarget(args, out AbstractTreeNode target, out DragDropTargetPosition position))
            {
                args.Effects = DragDropEffects.Move;
                args.Handled = true;

                FrameworkElement element = args.OriginalSource as FrameworkElement;
                TreeViewItem item = this.GetNearestContainer(element);

                args.Data.SetData("HighlightedItem", item);

                switch (position)
                {
                    case DragDropTargetPosition.Before:
                        item.BorderThickness = new Thickness(0, 2, 0, 0);
                        break;

                    case DragDropTargetPosition.After:
                        item.BorderThickness = new Thickness(0, 0, 0, 2);
                        break;
                }
            }
        }

        private void TreeView_Drop(object sender, DragEventArgs args)
        {
            args.Effects = DragDropEffects.None;
            args.Handled = true;

            if (this.TryComputeDropTarget(args, out AbstractTreeNode target, out DragDropTargetPosition position))
            {
                AbstractTreeNode nodeToDrag = (AbstractTreeNode)args.Data.GetData("DragSource");
                Debug.Assert(nodeToDrag != null, "nodeToDrag != null");

                this.MoveItem(nodeToDrag, target, position);

                if (args.Data.GetData("HighlightedItem") is TreeViewItem highlightedItem)
                {
                    highlightedItem.BorderThickness = new Thickness(0);
                }

                args.Effects = DragDropEffects.Move;
            }
        }

        private bool TryComputeDropTarget(DragEventArgs args, out AbstractTreeNode target, out DragDropTargetPosition position)
        {
            FrameworkElement source = args.OriginalSource as FrameworkElement;
            TreeViewItem item = this.GetNearestContainer(source);

            target = item?.DataContext as AbstractTreeNode;

            Debug.Assert(source != null, "source should not be null");
            Point relativePosition = args.GetPosition(source);

            if (relativePosition.Y - (source.ActualHeight / 2) < 0)
            {
                position = DragDropTargetPosition.Before;
            }
            else
            {
                position = DragDropTargetPosition.After;
            }

            AbstractTreeNode nodeToDrag = (AbstractTreeNode)args.Data.GetData("DragSource");
            Debug.Assert(nodeToDrag != null, "nodeToDrag != null");

            return target != null && this.CheckDropTarget(nodeToDrag, target);
        }

        private bool CheckDropTarget(AbstractTreeNode sourceItem, AbstractTreeNode targetItem)
        {
            // Check whether the target item is meeting your condition
            return sourceItem != targetItem && targetItem != null;
        }

        private void MoveItem(AbstractTreeNode sourceItem, AbstractTreeNode targetItem, DragDropTargetPosition targetPosition)
        {
            switch (targetPosition)
            {
                case DragDropTargetPosition.Before:
                    {
                        sourceItem.Parent.Children.Remove(sourceItem);
                        int indexOfNode = targetItem.Parent.Children.IndexOf(targetItem);
                        targetItem.Parent.Children.Insert(indexOfNode, sourceItem);
                        sourceItem.Parent = targetItem.Parent;
                    }

                    break;

                case DragDropTargetPosition.After:
                    {
                        if (targetItem is PresetFolderNode newParent)
                        {
                            sourceItem.Parent.Children.Remove(sourceItem);
                            newParent.Children.Insert(0, sourceItem);
                            sourceItem.Parent = newParent;
                        }
                        else if (targetItem is PresetNode nodeBeforeMe)
                        {
                            sourceItem.Parent.Children.Remove(sourceItem);
                            int indexOfNode = nodeBeforeMe.Parent.Children.IndexOf(nodeBeforeMe);
                            nodeBeforeMe.Parent.Children.Insert(indexOfNode + 1, sourceItem);
                            sourceItem.Parent = nodeBeforeMe.Parent;
                        }
                    }

                    break;
            }
        }

        private TreeViewItem GetNearestContainer(UIElement element)
        {
            // Walk up the element tree to the nearest tree view item.
            TreeViewItem container = element as TreeViewItem;
            while (container == null && element != null)
            {
                element = VisualTreeHelper.GetParent(element) as UIElement;
                container = element as TreeViewItem;
            }

            return container;
        }
    }
}
