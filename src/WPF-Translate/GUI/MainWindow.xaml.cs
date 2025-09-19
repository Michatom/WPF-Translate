using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;

namespace de.LandauSoftware.WPFTranslate
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
		private bool _isClosing = false;
		/// <summary>
		/// Erstellt ein neues Hauptfenster
		/// </summary>
		public MainWindow()
        {
            InitializeComponent();
			this.Closing += MainWindow_Closing;
			this.Loaded += MainWindow_LoadedSinglefire;
        }
		private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (_isClosing) return;

			e.Cancel = true; 

			var settings = new MetroDialogSettings
			{
				AffirmativeButtonText = (string)App.Current.FindResource("yes"),
				NegativeButtonText = (string)App.Current.FindResource("no"),
				DefaultButtonFocus = MessageDialogResult.Negative 
			};

			var result = await this.ShowMessageAsync((string)App.Current.FindResource("closeConfirmation"), (string)App.Current.FindResource("closeConfirmationText"), MessageDialogStyle.AffirmativeAndNegative, settings);

			if (result == MessageDialogResult.Affirmative)
			{
				_isClosing = true;
				this.Close();  // Ponownie wywołaj Close(), tym razem bez dialogu
			}
		}
		private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Erstellt eine neue Spalte mit einem Button
        /// </summary>
        /// <param name="content">Button Content</param>
        /// <param name="toolTip">Button Tooltip</param>
        /// <param name="commandBindingPath">Command Binding string</param>
        private void CreateButtonColoumn(string content, string toolTip, string commandBindingPath)
        {
            FrameworkElementFactory ff = new FrameworkElementFactory(typeof(Button));
            ff.SetValue(Button.ContentProperty, content);
            ff.SetValue(Button.VerticalAlignmentProperty, VerticalAlignment.Top);
            ff.SetValue(Button.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            ff.SetValue(Button.StyleProperty, App.Current.FindResource("AccentCircleButtonStyle"));
            ff.SetValue(Button.ToolTipProperty, toolTip);
            ff.SetBinding(Button.WidthProperty, new Binding(nameof(Button.ActualHeight)) { RelativeSource = new RelativeSource(RelativeSourceMode.Self), Mode = BindingMode.OneWay });
            ff.SetBinding(Button.CommandProperty, new Binding(nameof(BindingProxy.Data) + "." + commandBindingPath) { Source = Resources["BindingProxy"], Mode = BindingMode.OneWay });
            ff.SetBinding(Button.CommandParameterProperty, new Binding());

            GridViewColumn col = new GridViewColumn
            {
                CellTemplate = new DataTemplate
                {
                    DataType = typeof(Button),
                    VisualTree = ff
                }
            };

            gridView.Columns.Add(col);
        }

        /// <summary>
        /// Erstellt eine neue Spalte
        /// </summary>
        /// <param name="header">Header</param>
        /// <param name="cellTemplate">Cellen Vorlage</param>
        private void CreateColoumn(object header, DataTemplate cellTemplate)
        {
            GridViewColumn col = new GridViewColumn
            {
                Header = header?.ToString(),
                CellTemplate = cellTemplate
            };

            gridView.Columns.Add(col);
        }

		/// <summary>
		/// Erstellt eine neue Textzellen Vorlage.
		/// </summary>
		/// <param name="langKey">SprachKey</param>
		/// <param name="binding">Binding</param>
		/// <param name="allowNewLines">Neue zeilen und Tabs erlauben</param>
		/// <returns></returns>
		private DataTemplate CreateTextCellTemplate(string langKey, Binding binding, bool allowNewLines)
		{
			binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

			FrameworkElementFactory ff = new FrameworkElementFactory(typeof(TextBox));
			ff.SetBinding(TextBox.TextProperty, binding);
			ff.SetValue(TextBox.VerticalAlignmentProperty, VerticalAlignment.Stretch);
			ff.SetValue(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Top);
			ff.SetValue(TextBox.BackgroundProperty, null);

			if (langKey != null)
			{
				ff.SetValue(SpellCheck.IsEnabledProperty, true);
				ff.SetValue(TextBox.LanguageProperty, XmlLanguage.GetLanguage(langKey));
			}

			ff.SetValue(TextBox.AcceptsTabProperty, false);
			ff.SetValue(TextBox.AcceptsReturnProperty, allowNewLines);

			ff.AddHandler(TextBox.PreviewKeyDownEvent, new KeyEventHandler(TextBox_PreviewKeyDown));

			return new DataTemplate() { DataType = typeof(TextBox), VisualTree = ff };
		}

		private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Tab)
			{
				TextBox textBox = sender as TextBox;
				if (textBox != null)
				{
					ListViewItem listViewItem = FindAncestor<ListViewItem>(textBox);
					if (listViewItem != null)
					{
						ListView listView = FindAncestor<ListView>(listViewItem);
						if (listView != null)
						{
							var textBoxes = FindVisualChildren<TextBox>(listViewItem).ToList();
							int currentIndex = textBoxes.IndexOf(textBox);

							if (currentIndex < textBoxes.Count - 1)
							{
								textBoxes[currentIndex + 1].Focus();
								try
								{
									textBoxes[currentIndex + 1].Select(textBoxes[currentIndex + 1].Text.Length, 0);
								}
								catch { }
								e.Handled = true; 
							}
							else
							{
								int currentItemIndex = listView.Items.IndexOf(listViewItem.DataContext);
								if (currentItemIndex < listView.Items.Count - 1)
								{
									var nextListViewItem = listView.ItemContainerGenerator.ContainerFromIndex(currentItemIndex + 1) as ListViewItem;
									if (nextListViewItem != null)
									{
										var nextTextBoxes = FindVisualChildren<TextBox>(nextListViewItem).ToList();
										if (nextTextBoxes.Any())
										{
											nextTextBoxes[0].Focus();
											try
											{
												textBoxes[0].Select(textBoxes[currentIndex + 1].Text.Length, 0);
											}
											catch { }
											e.Handled = true;
										}
									}
								}
							}
						}
					}
				}
			}
		}

		private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
		{
			while (current != null && !(current is T))
			{
				current = System.Windows.Media.VisualTreeHelper.GetParent(current);
			}
			return current as T;
		}

		private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
		{
			if (depObj != null)
			{
				for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
				{
					DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
					if (child != null && child is T)
					{
						yield return (T)child;
					}

					foreach (T childOfChild in FindVisualChildren<T>(child))
					{
						yield return childOfChild;
					}
				}
			}
		}

		private void MainWindow_LoadedSinglefire(object sender, RoutedEventArgs e)
        {
            this.Loaded -= MainWindow_LoadedSinglefire;

            VModel_LanguageCollectionChangedEvent(this, EventArgs.Empty);
        }

        /// <summary>
        /// Wird beim hinzufügen, ändern oder löschen einer Sprache aufgerufen und updatet die
        /// Spalten des Fensters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VModel_LanguageCollectionChangedEvent(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                gridView.Columns.Clear();

                CreateButtonColoumn("X", App.FindString("remove"), nameof(MainWindowViewModel.RemoveKeyCommand));
                CreateButtonColoumn("T", App.FindString("translator"), nameof(MainWindowViewModel.TranslateKeyCommand));

                CreateColoumn("Key", CreateTextCellTemplate(null, new Binding(nameof(LangValueCollection.Key)), false));

                for (int i = 0; i < vModel.LangData.Languages.Count; i++)
                {
                    Language lang = vModel.LangData.Languages[i];

                    CreateColoumn(lang.LangKey, CreateTextCellTemplate(lang.LangKey, new Binding("[" + i + "]." + nameof(LangValue.Value)), true));
                }
            });
        }

        private void VModel_LanguageCollectionScrollIntoViewRequest(object sender, LangValueCollection e)
        {
            Keyboard.ClearFocus();

            listView.Focus();
            listView.ScrollIntoView(e);
            e.BlinkBackgroundForTwoSeconds();
        }

		private void ShowLicenses_Click(object sender, RoutedEventArgs e)
		{
			string tempFile = Path.GetTempFileName() + ".txt";
			using (var writer = new StreamWriter(tempFile, false))
			{
				writer.WriteLine("==== WPF-Translate ====");
				writer.WriteLine("Jan Wiesemann - It's simple. I don't care what you are doing with this. Just don't tell people it's yours and give me at least some credit.");
				writer.WriteLine("Additional GitHub contributors: @michatom");
				writer.WriteLine();
				writer.WriteLine();
				writer.WriteLine();
				writer.WriteLine("==== MahApps.Metro ====");
				writer.WriteLine("MIT License\r\n\r\nCopyright (c) .NET Foundation and Contributors. All rights reserved.\r\n\r\nPermission is hereby granted, free of charge, to any person obtaining a copy\r\nof this software and associated documentation files (the \"Software\"), to deal\r\nin the Software without restriction, including without limitation the rights\r\nto use, copy, modify, merge, publish, distribute, sublicense, and/or sell\r\ncopies of the Software, and to permit persons to whom the Software is\r\nfurnished to do so, subject to the following conditions:\r\n\r\nThe above copyright notice and this permission notice shall be included in all\r\ncopies or substantial portions of the Software.\r\n\r\nTHE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR\r\nIMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,\r\nFITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE\r\nAUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER\r\nLIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,\r\nOUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE\r\nSOFTWARE.");
				writer.WriteLine();
				writer.WriteLine("==== MahApps.Metro.IconPacks ====");
				writer.WriteLine("MIT License\r\n\r\nCopyright (c) MahApps, Jan Karger\r\n\r\nPermission is hereby granted, free of charge, to any person obtaining a copy\r\nof this software and associated documentation files (the \"Software\"), to deal\r\nin the Software without restriction, including without limitation the rights\r\nto use, copy, modify, merge, publish, distribute, sublicense, and/or sell\r\ncopies of the Software, and to permit persons to whom the Software is\r\nfurnished to do so, subject to the following conditions:\r\n\r\nThe above copyright notice and this permission notice shall be included in all\r\ncopies or substantial portions of the Software.\r\n\r\nTHE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR\r\nIMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,\r\nFITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE\r\nAUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER\r\nLIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,\r\nOUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE\r\nSOFTWARE.");
				writer.WriteLine();
				writer.WriteLine("==== ControlzEx ====");
				writer.WriteLine("The MIT License (MIT)\r\n\r\nCopyright (c) 2015-2019 Jan Karger, Bastian Schmidt\r\n\r\nPermission is hereby granted, free of charge, to any person obtaining a copy\r\nof this software and associated documentation files (the \"Software\"), to deal\r\nin the Software without restriction, including without limitation the rights\r\nto use, copy, modify, merge, publish, distribute, sublicense, and/or sell\r\ncopies of the Software, and to permit persons to whom the Software is\r\nfurnished to do so, subject to the following conditions:\r\n\r\nThe above copyright notice and this permission notice shall be included in all\r\ncopies or substantial portions of the Software.\r\n\r\nTHE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR\r\nIMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,\r\nFITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE\r\nAUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER\r\nLIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,\r\nOUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE\r\nSOFTWARE.");
				writer.WriteLine();
				writer.WriteLine("==== Newtonsoft.Json ====");
				writer.WriteLine("The MIT License (MIT)\r\n\r\nCopyright (c) 2007 James Newton-King\r\n\r\nPermission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the \"Software\"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:\r\n\r\nThe above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.\r\n\r\nTHE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.");
				writer.WriteLine();
				writer.WriteLine("==== Microsoft.Xaml.Behaviors ====");
				writer.WriteLine("The MIT License (MIT)\r\n\r\nCopyright (c) 2015 Microsoft\r\n\r\nPermission is hereby granted, free of charge, to any person obtaining a copy\r\nof this software and associated documentation files (the \"Software\"), to deal\r\nin the Software without restriction, including without limitation the rights\r\nto use, copy, modify, merge, publish, distribute, sublicense, and/or sell\r\ncopies of the Software, and to permit persons to whom the Software is\r\nfurnished to do so, subject to the following conditions:\r\n\r\nThe above copyright notice and this permission notice shall be included in all\r\ncopies or substantial portions of the Software.\r\n\r\nTHE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR\r\nIMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,\r\nFITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE\r\nAUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER\r\nLIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,\r\nOUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE\r\nSOFTWARE.");
				writer.WriteLine();
			}

			Process.Start("notepad.exe", tempFile);
		}
	}
}