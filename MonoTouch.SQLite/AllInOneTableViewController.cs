// 
// AllInOneTableViewController.cs
//  
// Author: Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2012 Jeffrey Stedfast
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 

using System;
using System.Drawing;
using System.Reflection;
using System.Collections.Generic;

using MonoTouch.ObjCRuntime;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace MonoTouch.SQLite {
	public abstract class AllInOneTableViewController : UITableViewController
	{
		static HashSet<Type> registeredTypes = new HashSet<Type> ();
		protected static float DefaultSearchBarHeight = 44.0f;
		
		UISearchDisplayController searchDisplayController;
		bool searchLoaded = false;
		bool canSearch = false;
		UISearchBar searchBar;
		float rowHeight = -1;
		bool loaded = false;

		static void RegisterType (Type type)
		{
			lock (registeredTypes) {
				if (registeredTypes.Contains (type))
					return;

				foreach (var method in type.GetMethods (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)) {
					if (method.DeclaringType == typeof (AllInOneTableViewController))
						continue;

					var export = (DynamicExportAttribute) Attribute.GetCustomAttribute (method, typeof (DynamicExportAttribute), true);
					if (export == null)
						continue;

					//Console.WriteLine ("Registering method {0}.{1}() with selector {2}", type.FullName, method.Name, export.Selector);

					Runtime.ConnectMethod (type, method, export.Export);
				}

				registeredTypes.Add (type);
			}
		}
		
		public AllInOneTableViewController (UITableViewStyle style, bool canSearch) : base (style)
		{
			ClearsSelectionOnViewWillAppear = false;
			CanSearch = canSearch;

			RegisterType (GetType ());
		}

		public AllInOneTableViewController (UITableViewStyle style) : this (style, false)
		{
		}

		public AllInOneTableViewController () : this (UITableViewStyle.Grouped)
		{
		}
		
		public bool AutoHideSearch {
			get; set;
		}

		public bool CanSearch {
			get { return canSearch; }
			set {
				if (loaded)
					throw new InvalidOperationException ("Cannot change the CanSearch property after the AllInOneTableViewController has been loaded.");

				canSearch = value;
			}
		}
		
		public override bool ClearsSelectionOnViewWillAppear {
			get; set;
		}
		
		public override UISearchDisplayController SearchDisplayController {
			get { return searchDisplayController; }
		}
		
		public string SearchPlaceholder {
			get; set;
		}

		protected float RowHeight {
			get { return rowHeight > 0 || !loaded ? rowHeight : TableView.RowHeight; }
			set {
				if (rowHeight == value)
					return;

				if (searchLoaded)
					SearchDisplayController.SearchResultsTableView.RowHeight = value;

				if (loaded)
					TableView.RowHeight = value;

				rowHeight = value;
			}
		}
		
		protected virtual UISearchBar CreateSearchBar ()
		{
			return new UISearchBar (new RectangleF (0, 0, TableView.Bounds.Width, DefaultSearchBarHeight));
		}
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			if (CanSearch) {
				searchBar = CreateSearchBar ();
				if (SearchPlaceholder != null)
					searchBar.Placeholder = SearchPlaceholder;

				searchDisplayController = new UISearchDisplayController (searchBar, this);
				SearchDisplayController.SearchResultsWeakDataSource = this;
				SearchDisplayController.SearchResultsWeakDelegate = this;
				SearchDisplayController.WeakDelegate = this;

				TableView.TableHeaderView = searchBar;
			}
			
			TableView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleTopMargin;
			TableView.AutosizesSubviews = true;

			TableView.WeakDataSource = this;
			TableView.WeakDelegate = this;

			if (rowHeight > 0)
				TableView.RowHeight = rowHeight;

			loaded = true;
		}
		
		public void HideSearchBar ()
		{
			if (CanSearch && TableView != null && TableView.ContentOffset.Y < searchBar.Frame.Height)
				TableView.ContentOffset = new PointF (0, searchBar.Frame.Height);
		}
		
		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);
			
			if (AutoHideSearch)
				HideSearchBar ();
		}
		
		#region UITableViewDataSource
		[Export ("numberOfSectionsInTableView:")]
		protected abstract int NumberOfSections (UITableView tableView);

		[Export ("tableView:numberOfRowsInSection:")]
		protected abstract int RowsInSection (UITableView tableView, int section);

		[Export ("tableView:cellForRowAtIndexPath:")]
		protected abstract UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath);

		[DynamicExport ("tableView:titleForHeaderInSection:")]
		protected virtual string TitleForHeader (UITableView tableView, int section)
		{
			return null;
		}

		[DynamicExport ("tableView:titleForFooterInSection:")]
		protected virtual string TitleForFooter (UITableView tableView, int section)
		{
			return null;
		}

		[DynamicExport ("tableView:canEditRowAtIndexPath:")]
		protected virtual bool CanEditRow (UITableView tableView, NSIndexPath indexPath)
		{
			return false;
		}

		[DynamicExport ("tableView:canMoveRowAtIndexPath:")]
		protected virtual bool CanMoveRow (UITableView tableView, NSIndexPath idnexPath)
		{
			return false;
		}

		[DynamicExport ("sectionIndexTitlesForTableView:")]
		protected virtual string [] SectionIndexTitles (UITableView tableView)
		{
			return new string [0];
		}
		
		[DynamicExport ("tableView:sectionForSectionIndexTitle:atIndex:")]
		protected virtual int SectionFor (UITableView tableView, string title, int atIndex)
		{
			return 0;
		}

		[DynamicExport ("tableView:commitEditingStyle:forRowAtIndexPath:")]
		protected virtual void CommitEditingStyle (UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath)
		{
		}

		[DynamicExport ("tableView:moveRowAtIndexPath:toIndexPath:")]
		protected virtual void MoveRow (UITableView tableView, NSIndexPath sourceIndexPath, NSIndexPath destinationIndexPath)
		{
		}
		#endregion
		
		#region UITableViewDelegate
		[DynamicExport ("tableView:accessoryButtonTappedForRowWithIndexPath:")]
		protected virtual void AccessoryButtonTapped (UITableView tableView, NSIndexPath indexPath)
		{
		}

		[DynamicExport ("tableView:targetIndexPathForMoveFromRowAtIndexPath:toProposedIndexPath:")]
		protected virtual NSIndexPath CustomizeMoveTarget (UITableView tableView, NSIndexPath sourceIndexPath, NSIndexPath proposedIndexPath)
		{
			return proposedIndexPath;
		}

		[DynamicExport ("tableView:shouldIndentWhileEditingRowAtIndexPath:")]
		protected virtual bool ShouldIndentWhileEditing (UITableView tableView, NSIndexPath indexPath)
		{
			return false;
		}

		[DynamicExport ("tableView:indentationLevelForRowAtIndexPath:")]
		protected virtual int IndentationLevel (UITableView tableView, NSIndexPath indexPath)
		{
			return 0;
		}

		[DynamicExport ("tableView:editingStyleForRowAtIndexPath:")]
		protected virtual UITableViewCellEditingStyle EditingStyleForRow (UITableView tableView, NSIndexPath indexPath)
		{
			return UITableViewCellEditingStyle.None;
		}

		[DynamicExport ("tableView:willDisplayCell:forRowAtIndexPath:")]
		protected virtual void WillDisplay (UITableView tableView, UITableViewCell cell, NSIndexPath indexPath)
		{
		}

		[DynamicExport ("tableView:willBeginEditingRowAtIndexPath:")]
		protected virtual void WillBeginEditing (UITableView tableView, NSIndexPath indexPath)
		{
		}

		[DynamicExport ("tableView:didEndEditingRowAtIndexPath:")]
		protected virtual void DidEndEditing (UITableView tableView, NSIndexPath indexPath)
		{
		}

		[DynamicExport ("tableView:viewForHeaderInSection:")]
		protected virtual UIView GetViewForHeader (UITableView tableView, int section)
		{
			return null;
		}

		[DynamicExport ("tableView:viewForFooterInSection:")]
		protected virtual UIView GetViewForFooter (UITableView tableView, int section)
		{
			return null;
		}

		[DynamicExport ("tableView:heightForHeaderInSection:")]
		protected virtual float GetHeightForHeader (UITableView tableView, int section)
		{
			return tableView.SectionHeaderHeight;
		}

		[DynamicExport ("tableView:heightForFooterInSection:")]
		protected virtual float GetHeightForFooter (UITableView tableView, int section)
		{
			return tableView.SectionFooterHeight;
		}

		[DynamicExport ("tableView:heightForRowAtIndexPath:")]
		protected virtual float GetHeightForRow (UITableView tableView, NSIndexPath indexPath)
		{
			return tableView.RowHeight;
		}

		[DynamicExport ("tableView:willSelectRowAtIndexPath:")]
		protected virtual NSIndexPath WillSelectRow (UITableView tableView, NSIndexPath indexPath)
		{
			return indexPath;
		}

		[DynamicExport ("tableView:didSelectRowAtIndexPath:")]
		protected virtual void RowSelected (UITableView tableView, NSIndexPath indexPath)
		{
		}

		[DynamicExport ("tableView:willDeselectRowAtIndexPath:")]
		protected virtual void WillDeselectRow (UITableView tableView, NSIndexPath indexPath)
		{
		}

		[DynamicExport ("tableView:didDeselectRowAtIndexPath:")]
		protected virtual void RowDeselected (UITableView tableView, NSIndexPath indexPath)
		{
		}

		[DynamicExport ("tableView:titleForDeleteConfirmationButtonForRowAtIndexPath:")]
		protected virtual string TitleForDeleteConfirmation (UITableView tableView, NSIndexPath indexPath)
		{
			return "Delete";
		}

		[DynamicExport ("tableView:shouldShowMenuForRowAtIndexPath:")]
		protected virtual bool ShouldShowMenu (UITableView tableView, NSIndexPath rowAtindexPath)
		{
			return false;
		}

		[DynamicExport ("tableView:canPerformAction:forRowAtIndexPath:withSender:")]
		protected virtual bool CanPerformAction (UITableView tableView, Selector action, NSIndexPath indexPath, NSObject sender)
		{
			return false;
		}

		[DynamicExport ("tableView:performAction:forRowAtIndexPath:withSender:")]
		protected void PerformAction (UITableView tableView, Selector action, NSIndexPath indexPath, NSObject sender)
		{
		}
		#endregion
		
		#region UISearchDisplayDelegate
		[DynamicExport ("searchDisplayControllerWillBeginSearch:")]
		protected virtual void WillBeginSearch (UISearchDisplayController controller)
		{
		}

		[DynamicExport ("searchDisplayControllerDidBeginSearch:")]
		protected virtual void DidBeginSearch (UISearchDisplayController controller)
		{
		}

		[DynamicExport ("searchDisplayControllerWillEndSearch:")]
		protected virtual void WillEndSearch (UISearchDisplayController controller)
		{
		}

		[DynamicExport ("searchDisplayControllerDidEndSearch:")]
		protected virtual void DidEndSearch (UISearchDisplayController controller)
		{
		}

		[Export ("searchDisplayController:didLoadSearchResultsTableView:")]
		protected virtual void DidLoadSearchResults (UISearchDisplayController controller, UITableView tableView)
		{
			tableView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleTopMargin;
			tableView.AutosizesSubviews = true;

			if (rowHeight > 0)
				tableView.RowHeight = rowHeight;

			searchLoaded = true;
		}

		[Export ("searchDisplayController:willUnloadSearchResultsTableView:")]
		protected virtual void WillUnloadSearchResults (UISearchDisplayController controller, UITableView tableView)
		{
			searchLoaded = false;
		}

		[DynamicExport ("searchDisplayController:willShowSearchResultsTableView:")]
		protected virtual void WillShowSearchResults (UISearchDisplayController controller, UITableView tableView)
		{
		}

		[DynamicExport ("searchDisplayController:didShowSearchResultsTableView:")]
		protected virtual void DidShowSearchResults (UISearchDisplayController controller, UITableView tableView)
		{
		}

		[DynamicExport ("searchDisplayController:willHideSearchResultsTableView:")]
		protected virtual void WillHideSearchResults (UISearchDisplayController controller, UITableView tableView)
		{
		}

		[DynamicExport ("searchDisplayController:didHideSearchResultsTableView:")]
		protected virtual void DidHideSearchResults (UISearchDisplayController controller, UITableView tableView)
		{
		}

		[DynamicExport ("searchDisplayController:shouldReloadTableForSearchScope:")]
		protected virtual bool ShouldReloadForSearchScope (UISearchDisplayController controller, int scope)
		{
			return true;
		}

		[DynamicExport ("searchDisplayController:shouldReloadTableForSearchString:")]
		protected virtual bool ShouldReloadForSearchString (UISearchDisplayController controller, string search)
		{
			return true;
		}
		#endregion
	}
}
