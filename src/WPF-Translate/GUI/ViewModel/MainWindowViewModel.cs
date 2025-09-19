﻿using de.LandauSoftware.WPFTranslate.IO;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace de.LandauSoftware.WPFTranslate
{
    /// <summary>
    /// MainWindowViewModel
    /// </summary>
    public class MainWindowViewModel : MetroNotifyBase
    {
        /// <summary>
        /// MahApps Settings für Dialog mit Ja und Nein
        /// </summary>
        public static readonly MetroDialogSettings DialogSettingsYesNo = new MetroDialogSettings() { AffirmativeButtonText = App.FindString("yes"), NegativeButtonText = App.FindString("no"), DefaultButtonFocus = MessageDialogResult.Negative };

        private RelayICommand _AddKeyCommand;
        private RelayICommand _AddLanguageCommand;
        private RelayICommand _ClearCommand;
        private Dictionary<Language, ResourceDictionaryFile> _FileList;
        private LanguageKeyValueCollection _LangData;
        private RelayICommand _LoadFileCommand;
        private RelayICommand<LangValueCollection> _RemoveKeyCommand;

        private RelayICommand _RemoveLanguageCommand;

        private RelayICommand _SaveFileCommand;

        private RelayICommand _SearchCommand;

        private RelayICommand<LangValueCollection> _TranslateKeyCommand;

        private RelayICommand _TranslateLanguageCommand;

        /// <summary>
        /// Wird beim Ändern einer Sprache aufgerufen
        /// </summary>
        public event EventHandler LanguageCollectionChangedEvent;

        /// <summary>
        /// Wird aufgerufen wenn die Anwendung ein Element in den Vordergrund scrollen soll
        /// </summary>
        public event EventHandler<LangValueCollection> LanguageCollectionScrollIntoViewRequest;

        /// <summary>
        /// Fügt einen neuen Key hinzu
        /// </summary>
        public ICommand AddKeyCommand
        {
            get
            {
                if (_AddKeyCommand == null)
                    _AddKeyCommand = new RelayICommand(p => LangData.Languages.Count > 0, p =>
                    {
                        LangValueCollection langValue = LangData.AddKey();

                        LanguageCollectionScrollIntoViewRequest?.Invoke(this, langValue);
                    });

                return _AddKeyCommand;
            }
        }

        /// <summary>
        /// Fügt eine neue Sprache hinzu
        /// </summary>
        public ICommand AddLanguageCommand
        {
            get
            {
                if (_AddLanguageCommand == null)
                    _AddLanguageCommand = new RelayICommand(async p =>
                   {
                       LanguageSetupWindow lsw = new LanguageSetupWindow();

                       if (lsw.ShowDialog() == true)
                       {
                           ResourceDictionaryFile rdf = new ResourceDictionaryFile(lsw.FileName, lsw.Reader);

                           await AddResourceDictionaryFileToLangData(rdf, lsw.LangID);
                       }
                   });

                return _AddLanguageCommand;
            }
        }

        /// <summary>
        /// Löscht den Inhalt der Anwendung
        /// </summary>
        public ICommand ClearCommand
        {
            get
            {
                if (_ClearCommand == null)
                    _ClearCommand = new RelayICommand(async p =>
                    {
                        MessageDialogResult res = await DialogCoordinator.ShowMessageAsync(this, App.FindString("remove"), App.FindString("warningClear"), MessageDialogStyle.AffirmativeAndNegative, DialogSettingsYesNo);

                        if (res == MessageDialogResult.Affirmative)
                        {
                            FileList.Clear();
                            LangData = null;
                        }
                    });

                return _ClearCommand;
            }
        }

        /// <summary>
        /// Beinhaltet alle geladenen Dateien
        /// </summary>
        public Dictionary<Language, ResourceDictionaryFile> FileList
        {
            get
            {
                if (_FileList == null)
                    _FileList = new Dictionary<Language, ResourceDictionaryFile>();

                return _FileList;
            }
        }

        /// <summary>
        /// Sprachdaten (Alle Daten für die ListView)
        /// </summary>
        public LanguageKeyValueCollection LangData
        {
            get
            {
                if (_LangData == null)
                {
                    _LangData = new LanguageKeyValueCollection();
                    _LangData.LanguagesChangedEvent += LangData_LanguagesChangedEvent;
                }

                return _LangData;
            }
            set
            {
                _LangData = value;

                RaisePropertyChanged(nameof(LangData));

                RaiseLanguageCollectionChangedEvent();
            }
        }

        /// <summary>
        /// Lädt eine Datei
        /// </summary>
        public ICommand LoadFileCommand
        {
            get
            {
                if (_LoadFileCommand == null)
                    _LoadFileCommand = new RelayICommand(async o =>
                   {
                       try
                       {
                           OpenFileDialog ofd = new OpenFileDialog
                           {
                               Multiselect = true,
                               Filter = Readers.GetFileDialogFilter(),
                               CheckFileExists = false
                           };

                           if (ofd.ShowDialog() != true)
                               return;

                           foreach (string file in ofd.FileNames)
                           {
                               if (FileListContainsFile(file))
                                   continue;

                               IResourceFileReader reader = Readers.FindFileReader(file);

                               ResourceDictionaryFile rdf = reader.Read(file);

                               string langID = reader.GetLanguageKey(rdf);

                               await AddResourceDictionaryFileToLangData(rdf, langID);
                           }

                           LangData.Keys = new ObservableCollection<LangValueCollection>(LangData.Keys.OrderBy(k => k.Key)); //Sortieren nach key
                       }
                       catch (Exception ex)
                       {
                           await DialogCoordinator.ShowMessageAsync(this, App.FindString("error"), ex.Message);
                       }
                   });

                return _LoadFileCommand;
            }
        }

        /// <summary>
        /// Entfernt einen Key
        /// </summary>
        public ICommand RemoveKeyCommand
        {
            get
            {
                if (_RemoveKeyCommand == null)
                    _RemoveKeyCommand = new RelayICommand<LangValueCollection>(async lk =>
                    {
                        MessageDialogResult res = await DialogCoordinator.ShowMessageAsync(this, App.FindString("remove"), App.FindString("warningRemoveSelected"), MessageDialogStyle.AffirmativeAndNegative, DialogSettingsYesNo);

                        if (res == MessageDialogResult.Affirmative)
                            LangData.RemoveKey(lk);
                    });

                return _RemoveKeyCommand;
            }
        }

        /// <summary>
        /// Entfernt eine Sprache
        /// </summary>
        public ICommand RemoveLanguageCommand
        {
            get
            {
                if (_RemoveLanguageCommand == null)
                    _RemoveLanguageCommand = new RelayICommand(p => LangData.Languages.Count > 0, p =>
                    {
                        RemoveLanguageWindow rlw = new RemoveLanguageWindow(FileList);

                        if (rlw.ShowDialog() == true)
                        {
                            Language lang = rlw.Selectedlanguage;

                            FileList.Remove(lang);
                            LangData.RemoveLanguage(lang);
                        }
                    });

                return _RemoveLanguageCommand;
            }
        }

        /// <summary>
        /// speichert alle Dateien
        /// </summary>
        public ICommand SaveFileCommand
        {
            get
            {
                if (_SaveFileCommand == null)
                    _SaveFileCommand = new RelayICommand(p => LangData.Languages.Count > 0, async p =>
                    {
                        foreach (KeyValuePair<Language, ResourceDictionaryFile> file in FileList)
                        {
                            try
                            {
                                IEnumerable<DictionaryRawEntry> entrys = LangData.GetLangEntrysAsDictionaryEntry(file.Key);

                                file.Value.Entrys.AddRange(entrys);

                                IResourceFileReader reader = file.Value.Reader;
                                IResourceFileWriter writer = reader.GetWriter();

                                writer.Write(file.Value);

                                file.Value.Entrys.Clear();
                            }
                            catch (Exception ex)
                            {
                                MessageDialogResult res = await DialogCoordinator.ShowMessageAsync(this, App.FindString("error"), string.Format(App.FindString("errorCanNotSave"), ex.Message), MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Weiter", NegativeButtonText = "Abbrechen" });

                                if (res == MessageDialogResult.Negative)
                                    return;
                            }
                        }
                    });

                return _SaveFileCommand;
            }
        }

        /// <summary>
        /// Öffnet die Suche und führt diese aus
        /// </summary>
        public ICommand SearchCommand
        {
            get
            {
                if (_SearchCommand == null)
                    _SearchCommand = new RelayICommand(p => LangData.Languages.Count > 0, async p =>
                    {
                        SearchWindow sw = new SearchWindow();

                        if (sw.ShowDialog() != true)
                            return;

                        SearchWindow.SearchModule searchModule = sw.CreateSearchModule();

                        ProgressDialogController progresscontroller = await DialogCoordinator.ShowProgressAsync(this, App.FindString("search"), App.FindString("pleaseWait"), true);

                        progresscontroller.Maximum = LangData.Keys.Count;

                        List<LangValueCollection> matches = new List<LangValueCollection>();

                        for (int i = 0; i < LangData.Keys.Count; i++)
                        {
                            progresscontroller.SetProgress(i);

                            if (searchModule.IsMatch(LangData.Keys[i]))
                                matches.Add(LangData.Keys[i]);
                            else if (progresscontroller.IsCanceled)
                                matches.Clear();
                        }

                        await progresscontroller.CloseAsync();

                        if (matches.Count > 1)
                            new SearchPresenter(LangData.Languages, matches, lvc => LanguageCollectionScrollIntoViewRequest?.Invoke(this, lvc)).Show();
                        else if (matches.Count == 1)
                            LanguageCollectionScrollIntoViewRequest?.Invoke(this, matches.First());
                    });

                return _SearchCommand;
            }
        }

        /// <summary>
        /// Öffnet einen Key im übersetzter
        /// </summary>
        public ICommand TranslateKeyCommand
        {
            get
            {
                if (_TranslateKeyCommand == null)
                    _TranslateKeyCommand = new RelayICommand<LangValueCollection>(p => LangData.Languages.Count > 0, lk =>
                    {
                        List<LangValueCollection> collection = new List<LangValueCollection>() { lk }; //Da der Übersetzter nur mit Listen arbeiten kann wird eine neue Liste mit einem eimnzelnem Element erstellt

                        TranslateWindow.ShowDialog(collection, LangData.Languages);
                    });

                return _TranslateKeyCommand;
            }
        }

        /// <summary>
        /// Öffnet den Translator zum übersetzten einer ganzen Sprache
        /// </summary>
        public ICommand TranslateLanguageCommand
        {
            get
            {
                if (_TranslateLanguageCommand == null)
                    _TranslateLanguageCommand = new RelayICommand(p => LangData.Languages.Count > 0, p => TranslateWindow.ShowDialog(LangData.Keys, LangData.Languages));

                return _TranslateLanguageCommand;
            }
        }

		/// <summary>
		/// Fügt ein Wörterbuch in die Sprachdaten hinzu
		/// </summary>
		/// <param name="rdf">ResourceDictionaryFile</param>
		/// <param name="langID">Sprach Key</param>
		/// <returns></returns>
		private async Task AddResourceDictionaryFileToLangData(ResourceDictionaryFile rdf, string langID)
		{
			string fileName = System.IO.Path.GetFileName(rdf.FileName);

			while (true)
			{
				if (string.IsNullOrWhiteSpace(langID))
				{
					// Jeśli klucz jest pusty, poproś użytkownika o podanie klucza
					langID = await DialogCoordinator.ShowInputAsync(
	                    this,
	                    App.FindString("error"),
	                    string.Format(App.FindString("errorNoLangKey"), fileName)
                    );
					if (string.IsNullOrWhiteSpace(langID))
						return;
					continue;
				}

				bool exists = LangData.ContainsLanguage(langID);
				if (!exists)
					break;

				// Jeśli język już istnieje, wyświetl szczegółowy komunikat
				langID = await DialogCoordinator.ShowInputAsync(
	                this,
	                App.FindString("error"),
	                string.Format(App.FindString("errorLangExists"), langID, fileName)
                );
				if (string.IsNullOrWhiteSpace(langID))
					return;
			}

			for (int i = rdf.Entrys.Count - 1; i >= 0; i--)
			{
				if (rdf.Entrys[i] is DictionaryStringEntry entry)
				{
					LangData.AddSetValue(langID, entry.Key, entry.Value);
					rdf.Entrys.RemoveAt(i);
				}
			}

			Language lang = LangData.GetLangByID(langID);
			if (lang == null)
				lang = LangData.AddLanguage(langID);

			rdf.RemoveAllStringRessoueces();
			FileList.Add(lang, rdf);
		}

		/// <summary>
		/// Prüft, ob eine Datei in der Dateiliste vorhanden ist
		/// </summary>
		/// <param name="filename">Dateiname</param>
		/// <returns></returns>
		private bool FileListContainsFile(string filename)
        {
            return FileList.Contains(kvp => kvp.Value.FileName == filename);
        }

        private void LangData_LanguagesChangedEvent(object sender, EventArgs e)
        {
            LanguageCollectionChangedEvent?.Invoke(sender, e);
        }

        private void RaiseLanguageCollectionChangedEvent()
        {
            LanguageCollectionChangedEvent?.Invoke(this, EventArgs.Empty);
        }

	}
}