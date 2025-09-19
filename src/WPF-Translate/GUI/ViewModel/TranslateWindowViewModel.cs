﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace de.LandauSoftware.WPFTranslate
{
    /// <summary>
    /// TranslateWindowViewModel
    /// </summary>
    public class TranslateWindowViewModel : MetroNotifyBase
    {
        private CancellationTokenSource _CancellationTokenSource;
        private bool _CancelOnException = true;
        private int _CurrentPosition = -1;
        private LangValueCollection _CurrentTranslationKey;
        private IList<LangValueCollection> _KeyList;
        private IList<Language> _Languages;
        private RelayICommand _RestCommand;
        private Language _SelectedSourceLanguage;
        private Language _SelectedTargetLanguage;
        private RelayICommand _StartCommand;
        private RelayICommand _StopCommand;
        private List<Language> _TargetLanguages;
        private bool _TranslateJustEmpty = true;
		private RelayICommand _TranslateAllCommand;

		/// <summary>
		/// Cancellation Token für das Abbrechen einer Operation
		/// </summary>
		public CancellationTokenSource CancellationTokenSource
		{
			get
			{
				return _CancellationTokenSource;
			}
			set
			{
				_CancellationTokenSource = value;
				RaisePropertyChanged(nameof(CancellationTokenSource));
				System.Windows.Input.CommandManager.InvalidateRequerySuggested();
			}
		}

		/// <summary>
		/// Legt fest, ob die Anwendung bei einem Fehler abbrechen soll
		/// </summary>
		public bool CancelOnException
        {
            get
            {
                return _CancelOnException;
            }
            set
            {
                _CancelOnException = value;

                RaisePropertyChanged(nameof(CancelOnException));
            }
        }

        /// <summary>
        /// Aktuelle Position
        /// </summary>
        public int CurrentPosition
        {
            get
            {
                return _CurrentPosition;
            }
            set
            {
                _CurrentPosition = value;

                RaisePropertyChanged(nameof(CurrentPosition));
                RaisePropertyChanged(nameof(CurrentPositionUINumber));
            }
        }

        /// <summary>
        /// Aktuelle Position + 1 für das Anzeigen auf der Oberfläche
        /// </summary>
        public int CurrentPositionUINumber
        {
            get
            {
                return _CurrentPosition + 1;
            }
        }

        /// <summary>
        /// Beinhaltet den aktuell zu übersetztenden Wert
        /// </summary>
        public LangValueCollection CurrentTranslationKey
        {
            get
            {
                return _CurrentTranslationKey;
            }
            set
            {
                _CurrentTranslationKey = value;

                RaisePropertyChanged(nameof(CurrentTranslationKey));
            }
        }

        /// <summary>
        /// Liste aller Keys
        /// </summary>
        public IList<LangValueCollection> KeyList
        {
            get
            {
                return _KeyList;
            }
            set
            {
                _KeyList = value;

                RaisePropertyChanged(nameof(KeyList));
            }
        }

        /// <summary>
        /// Liste aller Sprachen
        /// </summary>
        public IList<Language> Languages
        {
            get
            {
                return _Languages;
            }
            set
            {
                _Languages = value;

                RaisePropertyChanged(nameof(Languages));

                TargetLanguages = null;
            }
        }

        /// <summary>
        /// Setzt die Suche zurück
        /// </summary>
        public ICommand RestCommand
        {
            get
            {
                if (_RestCommand == null)
                    _RestCommand = new RelayICommand(p => CancellationTokenSource == null, p => Rest());

                return _RestCommand;
            }
        }

        /// <summary>
        /// Ausgewählte Quellsprache
        /// </summary>
        public Language SelectedSourceLanguage
        {
            get
            {
                return _SelectedSourceLanguage;
            }
            set
            {
                if (value != _SelectedSourceLanguage)
                    Rest();

                _SelectedSourceLanguage = value;

                RaisePropertyChanged(nameof(SelectedSourceLanguage));

                TargetLanguages = null;
            }
        }

        /// <summary>
        /// Ausgewählte Sielsprache
        /// </summary>
        public Language SelectedTargetLanguage
        {
            get
            {
                return _SelectedTargetLanguage;
            }
            set
            {
                if (value != _SelectedTargetLanguage)
                    Rest();

                _SelectedTargetLanguage = value;

                RaisePropertyChanged(nameof(SelectedTargetLanguage));
            }
        }

        /// <summary>
        /// Startet das Übersetzten
        /// </summary>
        public ICommand StartCommand
        {
            get
            {
                if (_StartCommand == null)
                    _StartCommand = new RelayICommand(p => CancellationTokenSource == null && SelectedSourceLanguage != null && SelectedTargetLanguage != null, p =>
                    {
                        if (KeyList.Count - 1 == CurrentPosition)
                            CurrentPosition = -1;

                        CancellationTokenSource = new CancellationTokenSource();

                        Task.Run(() =>
                        {
                            CancellationToken cancelToken = CancellationTokenSource.Token;

                            while (!cancelToken.IsCancellationRequested)
                            {
                                if (!TranslateNextElement())
                                {
                                    CurrentTranslationKey = null;

                                    break;
                                }
                            }

							System.Windows.Application.Current.Dispatcher.Invoke(() =>
							{
								CancellationTokenSource = null;
								RaisePropertyChanged(nameof(CancellationTokenSource));
							});
						}, CancellationTokenSource.Token);
                    });

                return _StartCommand;
            }
        }

		public ICommand TranslateAllCommand
		{
			get
			{
				if (_TranslateAllCommand == null)
					_TranslateAllCommand = new RelayICommand(
						p => CancellationTokenSource == null && SelectedSourceLanguage != null,
						p => TranslateAllToAllTargetLanguages());
				return _TranslateAllCommand;
			}
		}

		public void TranslateAllToAllTargetLanguages()
		{
			if (SelectedSourceLanguage == null || Languages == null)
				return;

			CancellationTokenSource = new CancellationTokenSource();
			CancellationToken cancelToken = CancellationTokenSource.Token;

			Task.Run(() =>
			{
				foreach (var key in KeyList)
				{
					if (cancelToken.IsCancellationRequested)
						break;

					LangValue sourceValue = key.FindValueByLang(SelectedSourceLanguage);
					if (sourceValue == null || string.IsNullOrWhiteSpace(sourceValue.Value))
						continue;

					foreach (var targetLang in Languages)
					{
						if (targetLang.Equals(SelectedSourceLanguage))
							continue;

						LangValue targetValue = key.FindValueByLang(targetLang);
						if (TranslateJustEmpty && targetValue != null && !string.IsNullOrWhiteSpace(targetValue.Value))
							continue;

						try
						{
							string resp = Translate.StringTranslate(sourceValue.Value, SelectedSourceLanguage.LangKey, targetLang.LangKey);
							if (targetValue != null)
								targetValue.Value = resp;
							else
								key.SetValue(targetLang, resp);
						}
						catch (Exception ex)
						{
							if (CancelOnException)
							{
								Task.Run(async () =>
								{
									await DialogCoordinator.ShowMessageAsync(this, App.FindString("error"), ex.Message);
								});
								return;
							}
						}
					}
				}
				System.Windows.Application.Current.Dispatcher.Invoke(() =>
				{
					CancellationTokenSource = null;
					RaisePropertyChanged(nameof(CancellationTokenSource));
				});
			}, CancellationTokenSource.Token);
		}

		/// <summary>
		/// Stopt das Übersetzten
		/// </summary>
		public ICommand StopCommand
        {
            get
            {
                if (_StopCommand == null)
                    _StopCommand = new RelayICommand(p => CancellationTokenSource != null, p =>
                    {
                        CancellationTokenSource.Cancel();
                        CancellationTokenSource = null;
                    });

                return _StopCommand;
            }
        }

        /// <summary>
        /// Liste für die Zielsprache Diese Liste wird automatisch über den Geter generiert
        /// </summary>
        public List<Language> TargetLanguages
        {
            get
            {
                if (_TargetLanguages == null && Languages != null && SelectedSourceLanguage != null)
                {
                    _TargetLanguages = new List<Language>();

                    foreach (Language item in Languages)
                    {
                        if (item != SelectedSourceLanguage)
                            _TargetLanguages.Add(item);
                    }
                }

                return _TargetLanguages;
            }
            private set
            {
                _TargetLanguages = value;

                RaisePropertyChanged(nameof(TargetLanguages));
            }
        }

        /// <summary>
        /// Legt fest oder ruft ab, ob nur leere Werte übersetzt werden sollen
        /// </summary>
        public bool TranslateJustEmpty
        {
            get
            {
                return _TranslateJustEmpty;
            }
            set
            {
                _TranslateJustEmpty = value;

                RaisePropertyChanged(nameof(TranslateJustEmpty));
            }
        }

        /// <summary>
        /// Setzt die Position und den Aktuellen Key zurück
        /// </summary>
        private void Rest()
        {
            CurrentPosition = -1;
            CurrentTranslationKey = null;
        }

        /// <summary>
        /// Übersetzt das nächste Element
        /// </summary>
        /// <returns></returns>
        private bool TranslateNextElement()
        {
            CurrentPosition = CurrentPosition + 1;

            if (CurrentPosition >= KeyList.Count)
                return false;

            CurrentTranslationKey = KeyList[CurrentPosition];

            LangValue targetValue = CurrentTranslationKey.FindValueByLang(SelectedTargetLanguage);

            if (TranslateJustEmpty && !string.IsNullOrWhiteSpace(targetValue.Value))
                return true;

            LangValue sourceValue = CurrentTranslationKey.FindValueByLang(SelectedSourceLanguage);

            if (string.IsNullOrWhiteSpace(sourceValue.Value))
                return true;

            try
            {
                string resp = Translate.StringTranslate(sourceValue.Value, SelectedSourceLanguage.LangKey, SelectedTargetLanguage.LangKey);

                targetValue.Value = resp;
            }
            catch (Exception ex)
            {
                if (CancelOnException)
                {
                    Task.Run(async () =>
                    {
                        await DialogCoordinator.ShowMessageAsync(this, App.FindString("error"), ex.Message);
                    });

                    return false;
                }
            }

            return true;
        }
    }
}