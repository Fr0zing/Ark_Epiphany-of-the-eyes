using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ArkTracker
{
	// The dashboard is intentionally XAML-free: the existing .NET Framework 4.8
	// command-line build can compile it without adding a separate XAML toolchain.
	// It is hosted by the stable tracker window while all visible controls are WPF.
	internal sealed class WpfSettingsDashboard : UserControl
	{
		private readonly TrackerViewSettings settings;
		private readonly Action apply;
		private readonly Func<string[]> getKnownStructureClasses;
		private readonly Func<string[]> getKnownDinoClasses;
		private readonly ContentControl pageHost = new ContentControl();
		private readonly TextBlock statusText = new TextBlock();
		private readonly Button espButton = new Button();
		private readonly Dictionary<string, Button> navigationButtons = new Dictionary<string, Button>(StringComparer.Ordinal);
		private readonly Dictionary<int, string> knownTribes = new Dictionary<int, string>();
		private readonly List<string[]> turretRows = new List<string[]>();
		private readonly List<string[]> actorRows = new List<string[]>();
		private readonly List<string[]> eventRows = new List<string[]>();
		private RowPool turretRowPool;
		private RowPool actorRowPool;
		private EventFeedPool eventFeedPool;
		private TextBox actorTableFilter;
		private TextBlock diagnosticsText;
		private TextBlock noglinStatusText;
		private string currentPage = "Обзор";
		private string structureClassCategoryFilter;
		private string currentNoglinStatus = "Ожидание данных быстрых слотов…";
		private string currentDiagnostics = "Ожидание первых замеров…";
		private int localTribe;
		private int quickPlayerCount;
		private int quickStructureCount;
		private DateTime lastStatusUpdate = DateTime.MinValue;
		private DateTime lastTurretUpdate = DateTime.MinValue;
		private DateTime lastActorUpdate = DateTime.MinValue;
		private DateTime lastEventFeedUpdate = DateTime.MinValue;
		private DateTime lastQuickCountsUpdate = DateTime.MinValue;
		private DateTime lastDiagnosticsUpdate = DateTime.MinValue;
		private bool menuOpen;

		internal event Action NotificationTestRequested;

		internal bool WantsActorRows { get { return string.Equals(currentPage, "Обзор", StringComparison.Ordinal); } }

		internal bool WantsTurretRows { get { return string.Equals(currentPage, "Турели", StringComparison.Ordinal); } }

		private static readonly Brush AppBackground = Brush("#10121D");
		private static readonly Brush Surface = Brush("#191B28");
		private static readonly Brush CardBrush = Brush("#24283A");
		private static readonly Brush CardHoverBrush = Brush("#30354A");
		private static readonly Brush Border = Brush("#454A62");
		private static readonly Brush Text = Brush("#F3F4FA");
		private static readonly Brush Muted = Brush("#A3A8BF");
		private static readonly Brush Accent = Brush("#EE7097");
		private static readonly Brush AccentSoft = Brush("#703B57");
		private static readonly Brush Live = Brush("#4BE69D");
		private static readonly ControlTemplate RoundedButtonTemplate = (ControlTemplate)XamlReader.Parse(
			"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='{x:Type Button}'>" +
			"<Border Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='10'>" +
			"<ContentPresenter HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}' VerticalAlignment='{TemplateBinding VerticalContentAlignment}' Margin='{TemplateBinding Padding}'/>" +
			"</Border></ControlTemplate>");

		internal WpfSettingsDashboard(TrackerViewSettings viewSettings, Action applySettings, Func<string[]> knownStructureClasses, Func<string[]> knownDinoClasses)
		{
			settings = viewSettings;
			apply = applySettings;
			getKnownStructureClasses = knownStructureClasses;
			getKnownDinoClasses = knownDinoClasses;
			Background = AppBackground;
			Content = BuildRoot();
			ShowPage("Обзор");
		}

		internal void UpdateLiveStatus(string text)
		{
			if ((DateTime.UtcNow - lastStatusUpdate).TotalMilliseconds < 350.0) return;
			lastStatusUpdate = DateTime.UtcNow;
			statusText.Text = string.IsNullOrWhiteSpace(text) ? "Подключение…" : text;
		}

		internal void SetMenuOpen(bool open)
		{
			menuOpen = open;
			UpdateEspButton();
		}

		internal void RefreshEspState()
		{
			UpdateEspButton();
		}

		internal void RefreshCurrentPage()
		{
			RunOnUi(delegate { ShowPage(currentPage); });
		}

		internal void RefreshStructureClasses()
		{
			if (string.Equals(currentPage, "Постройки", StringComparison.Ordinal)) RefreshCurrentPage();
		}

		internal void RefreshDinoClasses()
		{
			if (string.Equals(currentPage, "Существа", StringComparison.Ordinal)) RefreshCurrentPage();
		}

		internal void UpdateDiagnostics(string text)
		{
			if ((DateTime.UtcNow - lastDiagnosticsUpdate).TotalMilliseconds < 250.0) return;
			lastDiagnosticsUpdate = DateTime.UtcNow;
			RunOnUi(delegate
			{
				currentDiagnostics = string.IsNullOrWhiteSpace(text) ? "Ожидание первых замеров…" : text;
				if (diagnosticsText != null) diagnosticsText.Text = currentDiagnostics;
			});
		}

		// These hooks keep the WPF layer independent from TrackerSnapshot and the
		// WinForms compatibility host. The host can publish already prepared data.
		internal void UpdateKnownTribes(IDictionary<int, string> tribes, int ownTribe)
		{
			Dictionary<int, string> published = new Dictionary<int, string>();
			if (tribes != null)
				foreach (KeyValuePair<int, string> tribe in tribes)
					if (tribe.Key != 0) published[tribe.Key] = tribe.Value;
			RunOnUi(delegate
			{
				bool changed = localTribe != ownTribe || knownTribes.Count != published.Count;
				if (!changed)
				{
					foreach (KeyValuePair<int, string> tribe in published)
					{
						string name;
						if (!knownTribes.TryGetValue(tribe.Key, out name) || !string.Equals(name, tribe.Value, StringComparison.Ordinal)) { changed = true; break; }
					}
				}
				if (!changed) return;
				localTribe = ownTribe;
				knownTribes.Clear();
				foreach (KeyValuePair<int, string> tribe in published)
					knownTribes[tribe.Key] = string.IsNullOrWhiteSpace(tribe.Value) ? "Племя " + tribe.Key.ToString(CultureInfo.InvariantCulture) : tribe.Value.Trim();
				if (string.Equals(currentPage, "Племена и цвета", StringComparison.Ordinal)) ShowPage(currentPage);
			});
		}

		internal void UpdateTurretRows(string[][] rows)
		{
			if ((DateTime.UtcNow - lastTurretUpdate).TotalMilliseconds < 300.0) return;
			lastTurretUpdate = DateTime.UtcNow;
			List<string[]> published = new List<string[]>();
			if (rows != null)
				for (int i = 0; i < rows.Length && i < 500; i++)
					if (rows[i] != null) published.Add((string[])rows[i].Clone());
			RunOnUi(delegate
			{
				turretRows.Clear();
				turretRows.AddRange(published);
				if (WantsTurretRows) RefreshTurretTable();
			});
		}

		// Row contract: kind, name, level, tribe, distance, height, status.
		internal void UpdateActorRows(string[][] rows)
		{
			if ((DateTime.UtcNow - lastActorUpdate).TotalMilliseconds < 300.0) return;
			lastActorUpdate = DateTime.UtcNow;
			List<string[]> published = new List<string[]>();
			if (rows != null)
				for (int i = 0; i < rows.Length && i < 1000; i++)
					if (rows[i] != null) published.Add((string[])rows[i].Clone());
			RunOnUi(delegate
			{
				actorRows.Clear();
				actorRows.AddRange(published);
				if (WantsActorRows) RefreshActorTable();
			});
		}

		// Row contract: title, text, age label, severity name.
		internal void UpdateEventFeed(string[][] rows)
		{
			if ((DateTime.UtcNow - lastEventFeedUpdate).TotalMilliseconds < 300.0) return;
			lastEventFeedUpdate = DateTime.UtcNow;
			List<string[]> published = new List<string[]>();
			if (rows != null)
				for (int i = 0; i < rows.Length && i < 24; i++)
					if (rows[i] != null) published.Add((string[])rows[i].Clone());
			RunOnUi(delegate
			{
				eventRows.Clear();
				eventRows.AddRange(published);
				if (WantsActorRows) RefreshEventFeed();
			});
		}

		internal void UpdateQuickCounts(int players, int dinos, int structures)
		{
			if ((DateTime.UtcNow - lastQuickCountsUpdate).TotalMilliseconds < 300.0) return;
			lastQuickCountsUpdate = DateTime.UtcNow;
			RunOnUi(delegate
			{
				quickPlayerCount = players;
				quickStructureCount = structures;
				RefreshNavCounts();
			});
		}

		private void RefreshNavCounts()
		{
			Button playersButton;
			if (navigationButtons.TryGetValue("Игроки", out playersButton))
				playersButton.Content = quickPlayerCount > 0 ? "Игроки  ·  " + quickPlayerCount.ToString(CultureInfo.InvariantCulture) : "Игроки";
			Button structuresButton;
			if (navigationButtons.TryGetValue("Постройки", out structuresButton))
				structuresButton.Content = quickStructureCount > 0 ? "Постройки  ·  " + quickStructureCount.ToString(CultureInfo.InvariantCulture) : "Постройки";
		}

		internal void UpdateNoglinStatus(string text)
		{
			RunOnUi(delegate
			{
				currentNoglinStatus = string.IsNullOrWhiteSpace(text) ? "Ожидание данных быстрых слотов…" : text;
				if (noglinStatusText != null) noglinStatusText.Text = currentNoglinStatus;
			});
		}

		private UIElement BuildRoot()
		{
			Grid root = new Grid { Background = AppBackground };
			root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(74.0) });
			root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
			root.Children.Add(BuildHeader());

			Grid body = new Grid { Background = AppBackground };
			body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(214.0) });
			body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
			Border navigation = new Border { Background = Surface, BorderBrush = Border, BorderThickness = new Thickness(0, 0, 1, 0) };
			StackPanel links = new StackPanel { Margin = new Thickness(12, 18, 12, 12) };
			links.Children.Add(new TextBlock { Text = "НАВИГАЦИЯ", Foreground = Muted, FontSize = 10, FontWeight = FontWeights.SemiBold, Margin = new Thickness(12, 0, 0, 10) });
			AddNav(links, "Обзор");
			AddNav(links, "Игроки");
			AddNav(links, "Существа");
			AddNav(links, "Племена и цвета");
			AddNav(links, "Визуал");
			AddNav(links, "Постройки");
			AddNav(links, "Турели");
			AddNav(links, "Угрозы");
			AddNav(links, "Оповещения");
			AddNav(links, "Плавность");
			AddNav(links, "Расширенно");
			links.Children.Add(new Border { Height = 1, Background = Border, Margin = new Thickness(10, 16, 10, 12) });
			links.Children.Add(new TextBlock { Text = "INSERT / F10\nСкрыть или открыть меню", Foreground = Muted, FontSize = 11, LineHeight = 17, Margin = new Thickness(12, 0, 0, 0) });
			navigation.Child = new ScrollViewer { Content = links, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
			Grid.SetColumn(navigation, 0);
			body.Children.Add(navigation);

			Border content = new Border { Background = AppBackground, Padding = new Thickness(26, 20, 26, 20) };
			content.Child = pageHost;
			Grid.SetColumn(content, 1);
			body.Children.Add(content);
			Grid.SetRow(body, 1);
			root.Children.Add(body);
			return root;
		}

		private UIElement BuildHeader()
		{
			Border header = new Border { Background = Surface, BorderBrush = Border, BorderThickness = new Thickness(0, 0, 0, 1) };
			Grid grid = new Grid { Margin = new Thickness(22, 12, 22, 12) };
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210.0) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(142.0) });
			StackPanel brand = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
			brand.Children.Add(new TextBlock { Text = "ARK VISION", Foreground = Accent, FontSize = 22, FontWeight = FontWeights.Bold });
			brand.Children.Add(new TextBlock { Text = "визуальный помощник", Foreground = Muted, FontSize = 11, Margin = new Thickness(1, -2, 0, 0) });
			grid.Children.Add(brand);

			Border livePill = new Border { Background = CardBrush, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(11), Height = 34, Margin = new Thickness(0, 6, 18, 6), VerticalAlignment = VerticalAlignment.Center };
			StackPanel liveLine = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 12, 0) };
			liveLine.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = Live, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center });
			statusText.Text = "Подключение к игре…";
			statusText.Foreground = Live;
			statusText.FontSize = 12;
			statusText.FontWeight = FontWeights.SemiBold;
			statusText.TextTrimming = TextTrimming.CharacterEllipsis;
			statusText.VerticalAlignment = VerticalAlignment.Center;
			liveLine.Children.Add(statusText);
			livePill.Child = liveLine;
			Grid.SetColumn(livePill, 1);
			grid.Children.Add(livePill);

			ConfigureButton(espButton, 138, 36, true);
			espButton.Click += delegate
			{
				settings.OverlayEnabled = !settings.OverlayEnabled;
				Commit();
				if (string.Equals(currentPage, "Обзор", StringComparison.Ordinal)) RefreshCurrentPage();
			};
			Grid.SetColumn(espButton, 2);
			grid.Children.Add(espButton);
			header.Child = grid;
			UpdateEspButton();
			return header;
		}

		private void AddNav(Panel parent, string title)
		{
			Button button = new Button { Content = title, Height = 44, Margin = new Thickness(0, 0, 0, 6), HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(16, 0, 0, 0), FontSize = 14, FontWeight = FontWeights.SemiBold };
			ConfigureButton(button, 0, 44, false);
			button.Click += delegate { ShowPage(title); };
			navigationButtons[title] = button;
			parent.Children.Add(button);
		}

		private void ShowPage(string title)
		{
			currentPage = title;
			actorRowPool = null;
			turretRowPool = null;
			eventFeedPool = null;
			diagnosticsText = null;
			noglinStatusText = null;
			foreach (KeyValuePair<string, Button> item in navigationButtons)
			{
				bool selected = string.Equals(item.Key, title, StringComparison.Ordinal);
				item.Value.Background = selected ? AccentSoft : Surface;
				item.Value.Foreground = selected ? Brushes.White : Muted;
			}
			switch (title)
			{
				case "Игроки": pageHost.Content = PagePlayers(); break;
				case "Существа": pageHost.Content = PageCreatures(); break;
				case "Племена и цвета": pageHost.Content = PageTeamsAndColors(); break;
				case "Визуал": pageHost.Content = PageVisual(); break;
				case "Постройки": pageHost.Content = PageStructures(); break;
				case "Турели": pageHost.Content = PageTurrets(); break;
				case "Угрозы": pageHost.Content = PageThreats(); break;
				case "Оповещения": pageHost.Content = PageNotifications(); break;
				case "Плавность": pageHost.Content = PagePerformance(); break;
				case "Расширенно": pageHost.Content = PageAdvanced(); break;
				default: pageHost.Content = PageOverview(); break;
			}
		}

		private UIElement PageOverview()
		{
			WrapPanel cards = Page("Обзор", "Главные функции. Изменения применяются сразу.");
			cards.Children.Add(ToggleCard("Экранная ESP", "Главный переключатель подсветки", delegate { return settings.OverlayEnabled; }, delegate(bool value) { settings.OverlayEnabled = value; }));
			cards.Children.Add(ToggleCard("Мини-радар", "Компактная карта поверх игры", delegate { return settings.MiniRadarEnabled; }, delegate(bool value) { settings.MiniRadarEnabled = value; }));
			cards.Children.Add(ToggleCard("Игроки", "Рамки, имена и здоровье", delegate { return settings.ShowPlayers; }, delegate(bool value) { settings.ShowPlayers = value; }));
			cards.Children.Add(ToggleCard("Прирученные существа", "Показывать динозавров", delegate { return settings.ShowDinos; }, delegate(bool value) { settings.ShowDinos = value; }));
			cards.Children.Add(ToggleCard("Дикие существа", "Не засорять экран дикими", delegate { return settings.ShowWildDinos; }, delegate(bool value) { settings.ShowWildDinos = value; }));
			cards.Children.Add(ToggleCard("Постройки", "Точки и подписи базы", delegate { return settings.ShowStructures; }, delegate(bool value) { settings.ShowStructures = value; }));
			cards.Children.Add(EventFeedCard());
			cards.Children.Add(ActorTableCard());
			return Scroll(cards);
		}

		private UIElement PagePlayers()
		{
			WrapPanel cards = Page("Игроки", "Всё, что видно рядом с персонажем.");
			cards.Children.Add(ToggleCard("Показывать игроков", "Главный переключатель меток игроков", delegate { return settings.ShowPlayers; }, delegate(bool value) { settings.ShowPlayers = value; }));
			cards.Children.Add(ChoiceCard("Рамка", new string[] { "Без рамки", "Угловая рамка", "2D рамка" }, (int)settings.PlayerBox, delegate(int value) { settings.PlayerBox = (PlayerBoxStyle)value; }));
			cards.Children.Add(ToggleCard("Скелет", "Контур позы внутри рамки", delegate { return settings.ShowPlayerSkeleton; }, delegate(bool value) { settings.ShowPlayerSkeleton = value; }));
			cards.Children.Add(ToggleCard("Линия до игрока", "От низа экрана до цели, цвет как у самого игрока", delegate { return settings.ShowPlayerTracers; }, delegate(bool value) { settings.ShowPlayerTracers = value; }));
			cards.Children.Add(ToggleCard("Имена", "Ник персонажа", delegate { return settings.ShowPlayerNames; }, delegate(bool value) { settings.ShowPlayerNames = value; }));
			cards.Children.Add(ToggleCard("Оружие", "Предмет в руках", delegate { return settings.ShowPlayerWeapons; }, delegate(bool value) { settings.ShowPlayerWeapons = value; }));
			cards.Children.Add(ToggleCard("Здоровье", "Шкала и цифры", delegate { return settings.ShowPlayerHealth; }, delegate(bool value) { settings.ShowPlayerHealth = value; }));
			cards.Children.Add(ToggleCard("Игнорировать спящих", "Не рисовать спящих", delegate { return settings.IgnoreSleeping; }, delegate(bool value) { settings.IgnoreSleeping = value; }));
			cards.Children.Add(ToggleCard("Погибшие", "Отмечать тела отдельно", delegate { return settings.ShowCorpses; }, delegate(bool value) { settings.ShowCorpses = value; }));
			cards.Children.Add(ToggleCard("Название племени", "Показывать племя вместо номера", delegate { return settings.ShowTribeNames; }, delegate(bool value) { settings.ShowTribeNames = value; }));
			cards.Children.Add(ChoiceCard("Кого подсвечивать", new string[] { "Все вокруг", "Только враги", "Только свои", "Выбранные племена" }, TeamModeToChoice(settings.TeamMode), delegate(int value) { settings.TeamMode = ChoiceToTeamMode(value); }));
			cards.Children.Add(NumberCard("Толщина HP", "Ширина шкалы здоровья, px", settings.HealthBarThickness, 1, 14, delegate(double value) { settings.HealthBarThickness = (float)value; }));
			cards.Children.Add(NumberCard("Дальность игроков", "Максимальная дальность, м", settings.PlayerMaxDistanceCm / 100f, 10, 50000, delegate(double value) { settings.PlayerMaxDistanceCm = (float)value * 100f; }));
			return Scroll(cards);
		}

		private UIElement PageCreatures()
		{
			WrapPanel cards = Page("Существа", "Всё про прирученных и диких — в одном месте.");
			cards.Children.Add(ToggleCard("Показывать прирученных", "Независимо от диких — своё и чужое приручённое", delegate { return settings.ShowDinos; }, delegate(bool value) { settings.ShowDinos = value; }));
			cards.Children.Add(ToggleCard("Показывать диких", "Независимо от прирученных", delegate { return settings.ShowWildDinos; }, delegate(bool value) { settings.ShowWildDinos = value; }));
			cards.Children.Add(ToggleCard("Здоровье", "HP-полоса и числа рядом с существом, отдельно от игроков", delegate { return settings.ShowDinoHealth; }, delegate(bool value) { settings.ShowDinoHealth = value; }));
			cards.Children.Add(ToggleCard("Уровень", "Число уровня рядом с именем", delegate { return settings.ShowLevel; }, delegate(bool value) { settings.ShowLevel = value; }));
			cards.Children.Add(ToggleCard("Название племени", "У приручённых — то же самое, что и у игроков этого племени", delegate { return settings.ShowTribeNames; }, delegate(bool value) { settings.ShowTribeNames = value; }));
			cards.Children.Add(ToggleCard("Статусы", "Спит / движется / неподвижен", delegate { return settings.ShowStatuses; }, delegate(bool value) { settings.ShowStatuses = value; }));
			cards.Children.Add(NumberCard("Дальность существ", "Максимальная дальность, м", settings.DinoMaxDistanceCm / 100f, 10, 50000, delegate(double value) { settings.DinoMaxDistanceCm = (float)value * 100f; }));
			cards.Children.Add(ToggleCard("Показывать только важные виды", "Остальные виды скрыты полностью, если список ниже не пуст", delegate { return settings.ShowOnlyPriorityDinos; }, delegate(bool value) { settings.ShowOnlyPriorityDinos = value; }));
			cards.Children.Add(DinoSpeciesListCard());
			cards.Children.Add(InfoCard("Седло — пока нет", "Требует поиска и живой проверки чтения инвентаря/экипировки существа (та же неподтверждённая цепочка, что и для брони игрока) — офсет не проверен, поэтому не гадаем и не показываем."));
			return Scroll(cards);
		}

		private UIElement DinoSpeciesListCard()
		{
			Border card = LargeCard(976, 430);
			Grid layout = new Grid();
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(54) });
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			StackPanel heading = new StackPanel();
			heading.Children.Add(new TextBlock { Text = "Важные виды существ", Foreground = Text, FontSize = 15, FontWeight = FontWeights.SemiBold });
			heading.Children.Add(new TextBlock { Text = "Отмеченные виды получают полную подпись (имя, уровень, дистанция, ХП, статус). Остальные — только точка/рамка, без текста.", Foreground = Muted, FontSize = 11, Margin = new Thickness(0, 3, 0, 0) });
			layout.Children.Add(heading);

			string[] values = getKnownDinoClasses == null ? new string[0] : (getKnownDinoClasses() ?? new string[0]);
			TextBox search = new TextBox { Width = 360, Height = 29, Background = CardHoverBrush, Foreground = Text, BorderBrush = Border, Padding = new Thickness(9, 4, 9, 4), ToolTip = "Поиск по названию или внутреннему классу", HorizontalAlignment = HorizontalAlignment.Left };
			Button clearAll = new Button { Content = "СНЯТЬ ВСЕ ВАЖНЫЕ", Width = 190, Height = 29, Margin = new Thickness(372, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
			ConfigureButton(clearAll, 190, 29, false);
			clearAll.IsEnabled = settings.PriorityDinoClasses.Count > 0;
			Grid tools = new Grid(); tools.Children.Add(search); tools.Children.Add(clearAll);
			Grid.SetRow(tools, 1); layout.Children.Add(tools);

			ListBox list = new ListBox { Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = Text };
			Action rebuild = null;
			rebuild = delegate
			{
				list.Items.Clear();
				string query = (search.Text ?? string.Empty).Trim();
				for (int i = 0; i < values.Length; i++)
				{
					string[] parts = values[i].Split(new char[] { '\t' }, 2);
					string display = parts.Length > 0 ? parts[0] : values[i];
					string className = parts.Length > 1 ? parts[1] : values[i];
					if (query.Length > 0 && display.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) < 0 && className.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
					Grid row = new Grid { Height = 38 };
					row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
					row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
					TextBlock name = new TextBlock { Text = display, Foreground = Text, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = className };
					Button priority = new Button { Height = 27, HorizontalContentAlignment = HorizontalAlignment.Center };
					ConfigureButton(priority, 140, 27, false);
					Action refreshPriority = delegate
					{
						bool isPriority = settings.PriorityDinoClasses.Contains(className);
						priority.Content = isPriority ? "Важный вид ✓" : "Важный вид";
						priority.Background = isPriority ? Accent : CardHoverBrush;
						priority.Foreground = isPriority ? Brushes.White : Muted;
					};
					priority.Click += delegate
					{
						if (settings.PriorityDinoClasses.Contains(className)) settings.PriorityDinoClasses.Remove(className);
						else settings.PriorityDinoClasses.Add(className);
						Commit();
						refreshPriority();
						clearAll.IsEnabled = settings.PriorityDinoClasses.Count > 0;
					};
					refreshPriority();
					Grid.SetColumn(name, 0); Grid.SetColumn(priority, 1);
					row.Children.Add(name); row.Children.Add(priority);
					ListBoxItem item = new ListBoxItem { Content = row, Foreground = Text, Padding = new Thickness(0) };
					list.Items.Add(item);
				}
				if (list.Items.Count == 0) list.Items.Add(new ListBoxItem { Content = values.Length == 0 ? "Список заполнится после первого сканирования мира." : "По фильтру ничего не найдено.", Foreground = Muted, IsEnabled = false, Padding = new Thickness(8, 12, 0, 0) });
			};
			search.TextChanged += delegate { rebuild(); };
			clearAll.Click += delegate { settings.PriorityDinoClasses.Clear(); Commit(); ShowPage("Существа"); };
			rebuild();
			ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = list };
			Grid.SetRow(scroll, 2); layout.Children.Add(scroll);
			card.Child = layout;
			return card;
		}

		private UIElement PageTeamsAndColors()
		{
			WrapPanel cards = Page("Племена и цвета", "Выбирай найденные племена по названию. ID можно сохранить вручную, даже если племя сейчас не прогружено.");
			cards.Children.Add(ChoiceCard("Режим игроков", new string[] { "Все вокруг", "Только враги", "Только свои", "Выбранные племена" }, TeamModeToChoice(settings.TeamMode), delegate(int value) { settings.TeamMode = ChoiceToTeamMode(value); }));
			cards.Children.Add(TribeSelectionCard());
			cards.Children.Add(ColorCard("Здоровье", "Шкала и цифры HP", delegate { return settings.HealthColor; }, delegate(int value) { settings.HealthColor = value; }));
			cards.Children.Add(ColorCard("Своё племя", "Игроки твоего племени", delegate { return settings.OwnColor; }, delegate(int value) { settings.OwnColor = value; }));
			cards.Children.Add(ColorCard("Союзники", "Сохранённые союзные племена", delegate { return settings.AllyColor; }, delegate(int value) { settings.AllyColor = value; }));
			cards.Children.Add(ColorCard("Враги", "Вражеские игроки", delegate { return settings.EnemyColor; }, delegate(int value) { settings.EnemyColor = value; }));
			cards.Children.Add(ColorCard("Выбранные", "Племена из списка ниже", delegate { return settings.SelectedColor; }, delegate(int value) { settings.SelectedColor = value; }));
			cards.Children.Add(ColorCard("Погибшие", "Тела игроков", delegate { return settings.DeadColor; }, delegate(int value) { settings.DeadColor = value; }));
			cards.Children.Add(TeamListCard("Союзные ID", "Через запятую: 123, 456. Они не считаются врагами.", delegate { return settings.AllyTeams; }, SetManualAllies));
			cards.Children.Add(TeamListCard("Выбранные ID", "Через запятую: 123, 456. Работает в режиме «Выбранные племена».", delegate { return settings.SelectedTeams; }, SetManualSelectedTeams));
			return Scroll(cards);
		}

		private UIElement PageVisual()
		{
			WrapPanel cards = Page("Визуал", "Читаемость подписей и плавность overlay.");
			cards.Children.Add(ChoiceCard("Готовый профиль", new string[] { "Пользовательский", "PvP", "Пещеры", "База", "Минимальный" }, 0, ApplyVisualProfile));
			cards.Children.Add(ToggleCard("Умное масштабирование", "Дальние подписи становятся компактнее", delegate { return settings.AutoScale; }, delegate(bool value) { settings.AutoScale = value; }));
			cards.Children.Add(ToggleCard("Не перекрывать подписи", "Автоматически раздвигать текст", delegate { return settings.AvoidLabelOverlap; }, delegate(bool value) { settings.AvoidLabelOverlap = value; }));
			cards.Children.Add(ToggleCard("Фон текста", "Подложка для ярких сцен", delegate { return settings.TextBackground; }, delegate(bool value) { settings.TextBackground = value; }));
			cards.Children.Add(ToggleCard("Стрелки за экраном", "Направление на игроков вне кадра", delegate { return settings.OffscreenArrows; }, delegate(bool value) { settings.OffscreenArrows = value; }));
			cards.Children.Add(ToggleCard("Индикатор угрозы", "Враги и ближайшая дистанция", delegate { return settings.ThreatIndicator; }, delegate(bool value) { settings.ThreatIndicator = value; }));
			cards.Children.Add(ToggleCard("Статусы игроков", "Спит, двигается, неподвижен", delegate { return settings.ShowStatuses; }, delegate(bool value) { settings.ShowStatuses = value; }));
			cards.Children.Add(ToggleCard("Направление движения", "Короткая линия движения", delegate { return settings.ShowMovementDirection; }, delegate(bool value) { settings.ShowMovementDirection = value; }));
			cards.Children.Add(NumberCard("Размер текста", "Общий масштаб подписей", settings.TextSize, 7, 24, delegate(double value) { settings.TextSize = (float)value; }));
			cards.Children.Add(NumberCard("Обводка", "Толщина тёмного контура", settings.TextOutline, 0, 5, delegate(double value) { settings.TextOutline = (float)value; }));
			cards.Children.Add(NumberCard("Прозрачность текста", "От 20 до 100 процентов", settings.TextOpacity * 100f, 20, 100, delegate(double value) { settings.TextOpacity = (float)value / 100f; }));
			cards.Children.Add(NumberCard("Дальность существ", "Максимальная дальность, м", settings.DinoMaxDistanceCm / 100f, 10, 50000, delegate(double value) { settings.DinoMaxDistanceCm = (float)value * 100f; }));
			cards.Children.Add(NumberCard("Дальность построек", "Максимальная дальность, м", settings.StructureMaxDistanceCm / 100f, 10, 50000, delegate(double value) { settings.StructureMaxDistanceCm = (float)value * 100f; }));
			cards.Children.Add(NumberCard("Лимит FPS overlay", "0 — без ограничения", settings.OverlayFpsLimit, 0, 360, delegate(double value) { settings.OverlayFpsLimit = (int)value; }));
			cards.Children.Add(NumberCard("Компенсация движения", "Обычно достаточно 8–12 мс", settings.PositionPredictionMs, 0, 50, delegate(double value) { settings.PositionPredictionMs = (float)value; }));
			return Scroll(cards);
		}

		private UIElement PageStructures()
		{
			WrapPanel cards = Page("Постройки", "Управляй плотностью точек и подписями базы.");
			cards.Children.Add(ToggleCard("Показывать постройки", "Главный переключатель", delegate { return settings.ShowStructures; }, delegate(bool value) { settings.ShowStructures = value; }));
			cards.Children.Add(ToggleCard("Показывать свои постройки", "Выключи, чтобы видеть только чужие/незнакомые базы", delegate { return settings.ShowOwnStructures; }, delegate(bool value) { settings.ShowOwnStructures = value; }));
			cards.Children.Add(ToggleCard("Владелец в подписи", "Имя игрока, который поставил постройку (если известно и без группировки)", delegate { return settings.ShowStructureOwner; }, delegate(bool value) { settings.ShowStructureOwner = value; }));
			cards.Children.Add(ToggleCard("Трайб в подписи", "Название племени рядом с постройкой — отдельно от такого же переключателя у игроков", delegate { return settings.ShowStructureTribeNames; }, delegate(bool value) { settings.ShowStructureTribeNames = value; }));
			cards.Children.Add(ToggleCard("Группировать все группы", "Быстро применить режим ко всем категориям", delegate { return settings.GroupStructures; }, SetAllCategoryGrouping));
			cards.Children.Add(ChoiceCard("Подписи всех групп", new string[] { "Только точки", "Точки и названия групп", "Название каждой структуры" }, settings.StructureLabelMode, SetAllCategoryLabels));
			cards.Children.Add(NumberCard("Общий радиус группировки", "Применить расстояние ко всем категориям, м", settings.StructureGroupingDistanceCm / 100f, 1, 100, SetAllCategoryDistance));
			cards.Children.Add(NumberCard("Точек построек", "Ограничение для сложных баз", settings.MaxStructurePoints, 100, 10000, delegate(double value) { settings.MaxStructurePoints = (int)value; }));
			cards.Children.Add(NumberCard("Дальность построек", "Максимальная дальность, м", settings.StructureMaxDistanceCm / 100f, 10, 50000, delegate(double value) { settings.StructureMaxDistanceCm = (float)value * 100f; }));
			cards.Children.Add(StructureClassListCard());
			for (int i = 0; i < settings.StructureCategories.Count; i++) cards.Children.Add(CategoryCard(settings.StructureCategories[i]));
			return Scroll(cards);
		}

		private UIElement PageTurrets()
		{
			WrapPanel cards = Page("Турели", "Все найденные турели рядом. Отношение к племени указано прямо в таблице.");
			cards.Children.Add(TurretTableCard());
			StructureCategoryRule turrets = FindCategory("turrets");
			if (turrets != null) cards.Children.Add(CategoryCard(turrets));
			cards.Children.Add(ToggleCard("Дальность и режим на экране", "Подпись турели в игре: дальность и режим наведения, рядом с боезапасом", delegate { return settings.ShowTurretDetails; }, delegate(bool value) { settings.ShowTurretDetails = value; }));
			cards.Children.Add(TurretModeNamesCard());
			cards.Children.Add(ToggleCard("Оповещать о турелях", "Только при появлении чужой турели рядом", delegate { return settings.NotifyEnemyTurrets; }, delegate(bool value) { settings.NotifyEnemyTurrets = value; }));
			cards.Children.Add(NumberCard("Радиус турелей", "Дальность предупреждения, м", settings.TurretNoticeDistanceCm / 100f, 10, 5000, delegate(double value) { settings.TurretNoticeDistanceCm = (float)value * 100f; }));
			return Scroll(cards);
		}

		private UIElement PageThreats()
		{
			WrapPanel cards = Page("Угрозы", "Только вражеский приручённый Ноглин в выбранном радиусе.");
			cards.Children.Add(NoglinStateCard());
			cards.Children.Add(ToggleCard("Защита от Ноглина", "Следить за угрозой", delegate { return settings.NoglinProtection; }, delegate(bool value) { settings.NoglinProtection = value; }));
			cards.Children.Add(ToggleCard("Автоматически пить антидот", "Нажать слот, только когда ARK в фокусе", delegate { return settings.AutoUseAntidote; }, delegate(bool value) { settings.AutoUseAntidote = value; }));
			cards.Children.Add(ToggleCard("Звуковая тревога", "Сигнал при угрозе или отсутствии антидота", delegate { return settings.NoglinSound; }, delegate(bool value) { settings.NoglinSound = value; }));
			cards.Children.Add(NumberCard("Радиус Ноглина", "Рекомендуется 50–100 м", settings.NoglinDistanceCm / 100f, 10, 500, delegate(double value) { settings.NoglinDistanceCm = (float)value * 100f; }));
			cards.Children.Add(NumberCard("Повторная готовность", "После выхода угрозы, секунд", settings.NoglinRearmSeconds, 1, 120, delegate(double value) { settings.NoglinRearmSeconds = (int)value; }));
			cards.Children.Add(TextCard("Названия антидота", "Ключевые слова через | или запятую", settings.AntidoteKeywords, delegate(string value) { settings.AntidoteKeywords = value; }));
			return Scroll(cards);
		}

		private UIElement PageNotifications()
		{
			WrapPanel cards = Page("Оповещения", "Уведомления появляются поверх игры и не мешают управлению.");
			cards.Children.Add(ToggleCard("Оповещения", "Главный переключатель", delegate { return settings.NotificationsEnabled; }, delegate(bool value) { settings.NotificationsEnabled = value; }));
			cards.Children.Add(ToggleCard("Вражеские игроки", "Появление и исчезновение врагов", delegate { return settings.NotifyEnemyPlayers; }, delegate(bool value) { settings.NotifyEnemyPlayers = value; }));
			cards.Children.Add(ToggleCard("Своё племя", "События у игроков своего племени", delegate { return settings.NotifyOwnPlayers; }, delegate(bool value) { settings.NotifyOwnPlayers = value; }));
			cards.Children.Add(ToggleCard("Союзники", "Появление союзных племён", delegate { return settings.NotifyAlliedPlayers; }, delegate(bool value) { settings.NotifyAlliedPlayers = value; }));
			cards.Children.Add(ToggleCard("Неизвестные", "Если отношение к игроку не определено", delegate { return settings.NotifyUnknownPlayers; }, delegate(bool value) { settings.NotifyUnknownPlayers = value; }));
			cards.Children.Add(ToggleCard("Появился рядом", "Игрок вошёл в область прогруза", delegate { return settings.NotifyPlayerAppeared; }, delegate(bool value) { settings.NotifyPlayerAppeared = value; }));
			cards.Children.Add(ToggleCard("Пропал из области", "Без ложной тревоги при кратком сбое", delegate { return settings.NotifyPlayerDisappeared; }, delegate(bool value) { settings.NotifyPlayerDisappeared = value; }));
			cards.Children.Add(ToggleCard("Игрок погиб", "Отдельное сообщение о смерти", delegate { return settings.NotifyPlayerDied; }, delegate(bool value) { settings.NotifyPlayerDied = value; }));
			cards.Children.Add(ToggleCard("Игрок возродился", "Отдельное сообщение о возвращении", delegate { return settings.NotifyPlayerRevived; }, delegate(bool value) { settings.NotifyPlayerRevived = value; }));
			cards.Children.Add(ToggleCard("Игрок уснул", "Отдельно сообщить о сне", delegate { return settings.NotifyPlayerSlept; }, delegate(bool value) { settings.NotifyPlayerSlept = value; }));
			cards.Children.Add(ToggleCard("Игрок проснулся", "Отдельно сообщить о пробуждении", delegate { return settings.NotifyPlayerWoke; }, delegate(bool value) { settings.NotifyPlayerWoke = value; }));
			cards.Children.Add(ToggleCard("Близкий враг", "Предупредить при сближении", delegate { return settings.NotifyEnemyApproach; }, delegate(bool value) { settings.NotifyEnemyApproach = value; }));
			cards.Children.Add(ToggleCard("Опасная дистанция", "Отдельное тревожное предупреждение", delegate { return settings.NotifyEnemyDanger; }, delegate(bool value) { settings.NotifyEnemyDanger = value; }));
			cards.Children.Add(NumberCard("Радиус оповещения", "Дальность появления игрока, м", settings.EnemyNoticeDistanceCm / 100f, 10, 5000, delegate(double value) { settings.EnemyNoticeDistanceCm = (float)value * 100f; }));
			cards.Children.Add(NumberCard("Опасный радиус", "Более заметная тревога, м", settings.EnemyDangerDistanceCm / 100f, 5, 5000, delegate(double value) { settings.EnemyDangerDistanceCm = Math.Min(settings.EnemyNoticeDistanceCm, (float)value * 100f); }));
			cards.Children.Add(NumberCard("Радиус турелей", "Чужая турель рядом, м", settings.TurretNoticeDistanceCm / 100f, 10, 5000, delegate(double value) { settings.TurretNoticeDistanceCm = (float)value * 100f; }));
			cards.Children.Add(NumberCard("Задержка повторов", "Не повторять одинаковое, секунд", settings.NotificationCooldownSeconds, 1, 300, delegate(double value) { settings.NotificationCooldownSeconds = (int)value; }));
			cards.Children.Add(ChoiceCard("Положение ленты", new string[] { "Сверху слева", "Сверху справа", "Снизу слева", "Снизу справа" }, (int)settings.NotificationAnchor, delegate(int value) { settings.NotificationAnchor = (NotificationCorner)value; }));
			cards.Children.Add(NumberCard("Время показа", "Длительность одного сообщения, сек", settings.NotificationDurationMs / 1000.0, 1, 60, delegate(double value) { settings.NotificationDurationMs = (int)(value * 1000.0); }));
			cards.Children.Add(NumberCard("Сообщений на экране", "Ограничение высоты ленты", settings.MaxVisibleNotifications, 1, 12, delegate(double value) { settings.MaxVisibleNotifications = (int)value; }));
			cards.Children.Add(NumberCard("Подтверждение исчезновения", "Задержка перед сообщением, мс", settings.NotificationDisappearDelayMs, 100, 5000, delegate(double value) { settings.NotificationDisappearDelayMs = (int)value; }));
			cards.Children.Add(ActionCard("Проверить ленту", "Скрыть меню и показать тестовое оповещение", "ПОКАЗАТЬ", delegate
			{
				Action handler = NotificationTestRequested;
				if (handler != null) handler();
			}));
			return Scroll(cards);
		}

		private UIElement PagePerformance()
		{
			WrapPanel cards = Page("Плавность", "Настройки, которые сильнее всего влияют на FPS и задержку.");
			cards.Children.Add(NumberCard("Полное сканирование", "Новые объекты и данные, мс", settings.RefreshIntervalMs, 50, 60000, delegate(double value) { settings.RefreshIntervalMs = (int)value; }));
			cards.Children.Add(NumberCard("Метки игроков и существ", "Ограничение текста на экране", settings.MaxLabels, 10, 1000, delegate(double value) { settings.MaxLabels = (int)value; }));
			cards.Children.Add(NumberCard("Размер мини-радара", "Ширина и высота, px", settings.MiniRadarSize, 140, 500, delegate(double value) { settings.MiniRadarSize = (int)value; }));
			cards.Children.Add(NumberCard("Дальность существ", "Максимальная дальность, м", settings.DinoMaxDistanceCm / 100f, 10, 50000, delegate(double value) { settings.DinoMaxDistanceCm = (float)value * 100f; }));
			cards.Children.Add(NumberCard("Радиус угрозы", "Подсчёт ближайших врагов, м", settings.ThreatDistanceCm / 100f, 10, 50000, delegate(double value) { settings.ThreatDistanceCm = (float)value * 100f; }));
			cards.Children.Add(DiagnosticsCard());
			cards.Children.Add(InfoCard("Горячие клавиши", "F6 — ESP · F7 — скелет · F8 — имена · F9 — здоровье · F11 — только враги · Insert/F10 — меню"));
			return Scroll(cards);
		}

		private UIElement PageAdvanced()
		{
			WrapPanel cards = Page("Расширенно", "Остальные настройки считывания и overlay. Оставлены здесь, чтобы главный экран не был перегружен.");
			cards.Children.Add(TextCard("Фильтр объектов", "Часть имени или класса; пусто — без фильтра", settings.Search, delegate(string value) { settings.Search = value; }));
			cards.Children.Add(ToggleCard("Рамки существ", "Показывать рамки у объектов", delegate { return settings.ShowBoxes; }, delegate(bool value) { settings.ShowBoxes = value; }));
			cards.Children.Add(ToggleCard("Уровни", "Показывать уровень существа", delegate { return settings.ShowLevel; }, delegate(bool value) { settings.ShowLevel = value; }));
			cards.Children.Add(ToggleCard("Расстояние", "Показывать дистанцию до метки", delegate { return settings.ShowDistance; }, delegate(bool value) { settings.ShowDistance = value; }));
			cards.Children.Add(ToggleCard("Высота", "Показывать разницу высоты", delegate { return settings.ShowHeight; }, delegate(bool value) { settings.ShowHeight = value; }));
			cards.Children.Add(ToggleCard("Все классы построек", "Иначе рисуются только отмеченные классы", delegate { return settings.AllStructureClasses; }, SetAllStructureClasses));
			cards.Children.Add(ChoiceCard("Ориентация радара", new string[] { "По движению", "По камере", "Север сверху" }, (int)settings.Orientation, delegate(int value) { settings.Orientation = (RadarOrientation)value; }));
			cards.Children.Add(NumberCard("Масштаб мини-радара", "Радиус карты, м", settings.RadarZoomCm / 100f, 10, 50000, delegate(double value) { settings.RadarZoomCm = (float)value * 100f; }));
			cards.Children.Add(NumberCard("Дальность трупов", "Отдельное ограничение, м", settings.CorpseMaxDistanceCm / 100f, 10, 50000, delegate(double value) { settings.CorpseMaxDistanceCm = (float)value * 100f; }));
			return Scroll(cards);
		}

		private void ApplyVisualProfile(int profile)
		{
			if (profile <= 0) return;
			settings.AutoScale = true;
			settings.AvoidLabelOverlap = true;
			settings.OffscreenArrows = true;
			settings.ShowStatuses = true;
			settings.ShowMovementDirection = true;
			settings.ThreatIndicator = true;
			settings.TextOpacity = 1f;
			settings.TextOutline = 1.5f;
			settings.OverlayFpsLimit = 144;
			if (profile == 1)
			{
				settings.ShowPlayers = true; settings.ShowDinos = false; settings.ShowStructures = false;
				settings.ShowPlayerSkeleton = true; settings.ShowPlayerNames = true; settings.ShowPlayerHealth = true;
				settings.TeamMode = TeamFilterMode.Enemies; settings.TextSize = 12f; settings.TextOutline = 2f; settings.TextBackground = false;
				settings.PlayerMaxDistanceCm = 120000f; settings.ThreatDistanceCm = 25000f;
			}
			else if (profile == 2)
			{
				settings.ShowPlayers = true; settings.ShowDinos = true; settings.ShowStructures = false;
				settings.ShowPlayerSkeleton = true; settings.TeamMode = TeamFilterMode.All; settings.TextSize = 12f; settings.TextBackground = true;
				settings.PlayerMaxDistanceCm = 50000f; settings.DinoMaxDistanceCm = 50000f; settings.ThreatDistanceCm = 15000f;
			}
			else if (profile == 3)
			{
				settings.ShowPlayers = true; settings.ShowDinos = true; settings.ShowStructures = true;
				settings.ShowPlayerSkeleton = false; settings.TeamMode = TeamFilterMode.All; settings.TextSize = 10f; settings.TextBackground = false;
				settings.PlayerMaxDistanceCm = 40000f; settings.DinoMaxDistanceCm = 60000f; settings.StructureMaxDistanceCm = 60000f;
				settings.OverlayFpsLimit = 120;
			}
			else if (profile == 4)
			{
				settings.ShowPlayers = true; settings.ShowDinos = false; settings.ShowStructures = false;
				settings.ShowPlayerSkeleton = false; settings.ShowPlayerNames = true; settings.ShowPlayerWeapons = false; settings.ShowPlayerHealth = true;
				settings.TeamMode = TeamFilterMode.Enemies; settings.TextSize = 10f; settings.TextBackground = false;
				settings.ShowStatuses = false; settings.ShowMovementDirection = false; settings.ThreatIndicator = false;
				settings.PlayerMaxDistanceCm = 70000f; settings.MaxLabels = 100; settings.OverlayFpsLimit = 120;
			}
			Dispatcher.BeginInvoke(new Action(delegate { ShowPage(currentPage); }));
		}

		private WrapPanel Page(string title, string subtitle)
		{
			Grid shell = new Grid();
			shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(72.0) });
			shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
			StackPanel heading = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
			heading.Children.Add(new TextBlock { Text = title, Foreground = Text, FontSize = 28, FontWeight = FontWeights.Bold });
			heading.Children.Add(new TextBlock { Text = subtitle, Foreground = Muted, FontSize = 13, Margin = new Thickness(1, 2, 0, 0) });
			shell.Children.Add(heading);
			WrapPanel cards = new WrapPanel { Margin = new Thickness(-6, 0, 0, 0) };
			Grid.SetRow(cards, 1);
			shell.Children.Add(cards);
			cards.Tag = shell;
			return cards;
		}

		private UIElement Scroll(WrapPanel cards)
		{
			Grid shell = cards.Tag as Grid;
			// A horizontal ScrollViewer measures its child with infinite width.  That
			// prevents WrapPanel from making a second row and sends the remaining
			// controls off-screen.  Keep the page constrained to the actual window
			// width; individual wide tables have their own horizontal handling.
			ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = shell };
			return scroll;
		}

		private UIElement ToggleCard(string title, string subtitle, Func<bool> get, Action<bool> set)
		{
			Button toggle = new Button { Width = 74, Height = 30, VerticalAlignment = VerticalAlignment.Center };
			Action refresh = delegate
			{
				bool value = get();
				toggle.Content = value ? "ВКЛ" : "ВЫКЛ";
				toggle.Background = value ? Accent : CardHoverBrush;
				toggle.Foreground = value ? Brushes.White : Muted;
			};
			toggle.Click += delegate { set(!get()); Commit(); refresh(); };
			ConfigureButton(toggle, 74, 30, false);
			refresh();
			return Card(title, subtitle, toggle);
		}

		private UIElement ChoiceCard(string title, string[] options, int selected, Action<int> set)
		{
			int index = Math.Max(0, Math.Min(options.Length - 1, selected));
			Grid picker = new Grid { Width = 166, Height = 30 };
			Button trigger = new Button { Height = 30, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(11, 0, 9, 0) };
			ConfigureButton(trigger, 166, 30, false);
			Popup popup = new Popup { Placement = PlacementMode.Bottom, PlacementTarget = trigger, StaysOpen = false, AllowsTransparency = true };
			Border popupBorder = new Border { Background = CardBrush, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(5), Margin = new Thickness(0, 5, 0, 0), Width = 236 };
			StackPanel optionsPanel = new StackPanel();
			popupBorder.Child = optionsPanel;
			popup.Child = popupBorder;
			Action refresh = delegate { trigger.Content = options[index] + "   ▾"; };
			for (int i = 0; i < options.Length; i++)
			{
				int optionIndex = i;
				Button option = new Button { Content = options[i], Height = 34, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(11, 0, 8, 0), Margin = new Thickness(0, 1, 0, 1) };
				ConfigureButton(option, 0, 34, false);
				option.Click += delegate
				{
					index = optionIndex;
					set(index);
					Commit();
					refresh();
					popup.IsOpen = false;
				};
				optionsPanel.Children.Add(option);
			}
			trigger.Click += delegate { popup.IsOpen = !popup.IsOpen; };
			picker.Children.Add(trigger);
			picker.Children.Add(popup);
			refresh();
			return Card(title, "Выберите подходящий режим", picker);
		}

		private UIElement NumberCard(string title, string subtitle, double current, double minimum, double maximum, Action<double> set)
		{
			TextBox input = new TextBox { Width = 106, Height = 30, Text = current.ToString("0.##", CultureInfo.InvariantCulture), Foreground = Text, Background = CardHoverBrush, BorderBrush = Border, Padding = new Thickness(8, 4, 8, 4), HorizontalContentAlignment = HorizontalAlignment.Right };
			input.LostKeyboardFocus += delegate
			{
				double value;
				if (!double.TryParse(input.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value)) value = current;
				value = Math.Max(minimum, Math.Min(maximum, value));
				input.Text = value.ToString("0.##", CultureInfo.InvariantCulture);
				set(value);
				Commit();
			};
			input.KeyDown += delegate(object sender, KeyEventArgs args) { if (args.Key == Key.Enter) Keyboard.ClearFocus(); };
			return Card(title, subtitle, input);
		}

		private UIElement Card(string title, string subtitle, UIElement value)
		{
			Border card = new Border { Width = 316, Height = 108, Margin = new Thickness(6), Background = CardBrush, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(16, 12, 14, 12) };
			Grid layout = new Grid();
			layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
			layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			StackPanel words = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
			words.Children.Add(new TextBlock { Text = title, Foreground = Text, FontSize = 14, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
			words.Children.Add(new TextBlock { Text = subtitle, Foreground = Muted, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 8, 0), MaxHeight = 34 });
			layout.Children.Add(words);
			FrameworkElement element = value as FrameworkElement;
			if (element != null) element.VerticalAlignment = VerticalAlignment.Center;
			Grid.SetColumn(value, 1);
			layout.Children.Add(value);
			card.Child = layout;
			return card;
		}

		private UIElement InfoCard(string title, string text)
		{
			Border card = new Border { Width = 646, Height = 108, Margin = new Thickness(6), Background = CardBrush, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(16, 12, 16, 12) };
			StackPanel panel = new StackPanel();
			panel.Children.Add(new TextBlock { Text = title, Foreground = Text, FontSize = 14, FontWeight = FontWeights.SemiBold });
			panel.Children.Add(new TextBlock { Text = text, Foreground = Muted, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 7, 0, 0) });
			card.Child = panel;
			return card;
		}

		private UIElement DiagnosticsCard()
		{
			Border card = LargeCard(646, 230);
			StackPanel panel = new StackPanel();
			panel.Children.Add(new TextBlock { Text = "Живая диагностика", Foreground = Text, FontSize = 15, FontWeight = FontWeights.SemiBold });
			panel.Children.Add(new TextBlock { Text = "Последнее значение / пик за окно. Пик кадра выше 13,9 мс означает заметный пропуск на 144 Гц.", Foreground = Muted, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 10) });
			diagnosticsText = new TextBlock { Text = currentDiagnostics, Foreground = Live, FontSize = 12, LineHeight = 19, TextWrapping = TextWrapping.Wrap };
			panel.Children.Add(diagnosticsText);
			card.Child = panel;
			return card;
		}

		private UIElement ColorCard(string title, string subtitle, Func<int> get, Action<int> set)
		{
			return Card(title, subtitle + " · нажми, чтобы выбрать", ColorPicker(get, set, 106));
		}

		// A curated palette rather than a full HSV wheel - keeps the popup a single
		// glance-and-click grid instead of another fiddly control, which is what was
		// actually requested (no manual hex typing). The hex field survives underneath
		// only as an optional escape hatch for an exact match to something else.
		private static readonly int[] ColorPalette = new int[]
		{
			0xFF5364, 0xE74C3C, 0xFF8A65, 0xE8A33D, 0xFFC857, 0xF1C40F,
			0x4CD137, 0x2ECC71, 0x1ABC9C, 0x42E8D5, 0x00E5FF, 0x3498DB,
			0x57A6FF, 0x2E86FF, 0x9B59B6, 0xB77CFF, 0xFF6FB0, 0xFF3D71,
			0xFFFFFF, 0xA7B0BE, 0x95A5A6, 0x34495E, 0x8D6E63, 0x000000
		};

		private UIElement ColorPicker(Func<int> get, Action<int> set, double triggerWidth)
		{
			Button trigger = new Button { Width = triggerWidth, Height = 30, HorizontalContentAlignment = HorizontalAlignment.Center };
			ConfigureButton(trigger, triggerWidth, 30, false);
			Action refreshTrigger = delegate
			{
				int value = get() & 0xFFFFFF;
				trigger.Content = "#" + value.ToString("X6", CultureInfo.InvariantCulture);
				trigger.Background = ColorBrush(value);
				trigger.Foreground = ReadableTextColor(value);
			};

			Popup popup = new Popup { Placement = PlacementMode.Bottom, PlacementTarget = trigger, StaysOpen = false, AllowsTransparency = true };
			WrapPanel swatches = new WrapPanel { Width = 216 };
			for (int i = 0; i < ColorPalette.Length; i++)
			{
				int preset = ColorPalette[i];
				Button swatch = new Button { Width = 32, Height = 32, Margin = new Thickness(2), Background = ColorBrush(preset), BorderBrush = Border, BorderThickness = new Thickness(1) };
				swatch.Click += delegate { set(preset); Commit(); refreshTrigger(); popup.IsOpen = false; };
				swatches.Children.Add(swatch);
			}
			TextBox custom = new TextBox { Width = 216, Height = 28, Margin = new Thickness(0, 8, 0, 0), Foreground = Text, Background = CardHoverBrush, BorderBrush = Border, Padding = new Thickness(7, 3, 7, 3), HorizontalContentAlignment = HorizontalAlignment.Center };
			Action refreshCustom = delegate { custom.Text = "#" + (get() & 0xFFFFFF).ToString("X6", CultureInfo.InvariantCulture); };
			custom.LostKeyboardFocus += delegate
			{
				int value;
				if (TryReadColor(custom.Text, out value)) { set(value); Commit(); refreshTrigger(); }
				refreshCustom();
			};
			custom.KeyDown += delegate(object sender, KeyEventArgs args) { if (args.Key == Key.Enter) Keyboard.ClearFocus(); };
			refreshCustom();
			StackPanel popupContent = new StackPanel { Margin = new Thickness(2) };
			popupContent.Children.Add(swatches);
			popupContent.Children.Add(new TextBlock { Text = "Свой код (необязательно)", Foreground = Muted, FontSize = 10, Margin = new Thickness(2, 8, 0, 2) });
			popupContent.Children.Add(custom);
			Border popupBorder = new Border { Background = CardBrush, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Padding = new Thickness(8), Width = 236, Child = popupContent };
			popup.Child = popupBorder;
			trigger.Click += delegate { popup.IsOpen = !popup.IsOpen; };
			refreshTrigger();

			Grid host = new Grid { Width = triggerWidth, Height = 30 };
			host.Children.Add(trigger);
			host.Children.Add(popup);
			return host;
		}

		private static Brush ReadableTextColor(int color)
		{
			byte red = (byte)((color >> 16) & 0xFF), green = (byte)((color >> 8) & 0xFF), blue = (byte)(color & 0xFF);
			double luminance = 0.299 * red + 0.587 * green + 0.114 * blue;
			return luminance > 150.0 ? Brushes.Black : Brushes.White;
		}

		private UIElement TeamListCard(string title, string subtitle, Func<HashSet<int>> get, Action<HashSet<int>> set)
		{
			TextBox input = new TextBox { Width = 166, Height = 30, Text = JoinTeams(get()), Foreground = Text, Background = CardHoverBrush, BorderBrush = Border, Padding = new Thickness(8, 4, 8, 4), HorizontalContentAlignment = HorizontalAlignment.Left };
			input.LostKeyboardFocus += delegate
			{
				HashSet<int> teams = ReadTeams(input.Text);
				input.Text = JoinTeams(teams);
				set(teams);
				Commit();
			};
			input.KeyDown += delegate(object sender, KeyEventArgs args) { if (args.Key == Key.Enter) Keyboard.ClearFocus(); };
			return Card(title, subtitle, input);
		}

		private UIElement TextCard(string title, string subtitle, string current, Action<string> set)
		{
			TextBox input = new TextBox { Width = 166, Height = 30, Text = current ?? string.Empty, Foreground = Text, Background = CardHoverBrush, BorderBrush = Border, Padding = new Thickness(8, 4, 8, 4) };
			Action save = delegate
			{
				set((input.Text ?? string.Empty).Trim());
				Commit();
			};
			input.LostKeyboardFocus += delegate { save(); };
			input.KeyDown += delegate(object sender, KeyEventArgs args) { if (args.Key == Key.Enter) { save(); Keyboard.ClearFocus(); } };
			return Card(title, subtitle, input);
		}

		private UIElement ActionCard(string title, string subtitle, string buttonText, Action action)
		{
			Button button = new Button { Content = buttonText, Width = 106, Height = 30 };
			ConfigureButton(button, 106, 30, true);
			button.Click += delegate { if (action != null) action(); };
			return Card(title, subtitle, button);
		}

		private UIElement TribeSelectionCard()
		{
			Border card = LargeCard(976, 392);
			Grid layout = new Grid();
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			StackPanel heading = new StackPanel();
			heading.Children.Add(new TextBlock { Text = "Найденные племена", Foreground = Text, FontSize = 15, FontWeight = FontWeights.SemiBold });
			heading.Children.Add(new TextBlock { Text = "Союзники получают отдельный цвет и не считаются врагами. «Выбран» используется фильтром выбранных племён.", Foreground = Muted, FontSize = 11, Margin = new Thickness(0, 5, 0, 0) });
			layout.Children.Add(heading);

			Dictionary<int, string> rows = new Dictionary<int, string>(knownTribes);
			foreach (int team in settings.AllyTeams) if (!rows.ContainsKey(team)) rows[team] = "Сохранённое племя " + team.ToString(CultureInfo.InvariantCulture);
			foreach (int team in settings.SelectedTeams) if (!rows.ContainsKey(team)) rows[team] = "Сохранённое племя " + team.ToString(CultureInfo.InvariantCulture);
			StackPanel list = new StackPanel();
			if (rows.Count == 0)
			{
				list.Children.Add(new TextBlock { Text = "Племена появятся здесь после первого сканирования игроков и построек.", Foreground = Muted, FontSize = 12, Margin = new Thickness(8, 18, 0, 0) });
			}
			else
			{
				foreach (KeyValuePair<int, string> tribe in rows.OrderBy(delegate(KeyValuePair<int, string> value) { return value.Key == localTribe ? 0 : 1; }).ThenBy(delegate(KeyValuePair<int, string> value) { return value.Value; }, StringComparer.CurrentCultureIgnoreCase))
				{
					int team = tribe.Key;
					Grid row = new Grid { Height = 45, Margin = new Thickness(0, 0, 0, 3), Background = CardHoverBrush };
					row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
					row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(108) });
					row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(108) });
					StackPanel identity = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 8, 0) };
					identity.Children.Add(new TextBlock { Text = tribe.Value + (team == localTribe ? "  · СВОЁ" : string.Empty), Foreground = team == localTribe ? Live : Text, FontSize = 12, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
					identity.Children.Add(new TextBlock { Text = "ID " + team.ToString(CultureInfo.InvariantCulture), Foreground = Muted, FontSize = 10 });
					row.Children.Add(identity);

					Button ally = new Button { Width = 96, Height = 28, IsEnabled = team != localTribe };
					ConfigureButton(ally, 96, 28, false);
					Action refreshAlly = delegate
					{
						bool selected = settings.AllyTeams.Contains(team);
						ally.Content = selected ? "СОЮЗ  ✓" : "СОЮЗ";
						ally.Background = selected ? Brush("#237B8D") : Surface;
						ally.Foreground = selected ? Brushes.White : Muted;
					};
					ally.Click += delegate
					{
						if (settings.AllyTeams.Contains(team)) settings.AllyTeams.Remove(team);
						else
						{
							settings.AllyTeams.Add(team);
							settings.SelectedTeams.Remove(team);
						}
						settings.CustomTeam = settings.SelectedTeams.Count == 0 ? 0 : FirstTeam(settings.SelectedTeams);
						Commit();
						RefreshTeamsPageLater();
					};
					refreshAlly();
					Grid.SetColumn(ally, 1); row.Children.Add(ally);

					Button selectedTeam = new Button { Width = 96, Height = 28 };
					ConfigureButton(selectedTeam, 96, 28, false);
					Action refreshSelected = delegate
					{
						bool selected = settings.SelectedTeams.Contains(team);
						selectedTeam.Content = selected ? "ВЫБРАН  ✓" : "ВЫБРАТЬ";
						selectedTeam.Background = selected ? AccentSoft : Surface;
						selectedTeam.Foreground = selected ? Brushes.White : Muted;
					};
					selectedTeam.Click += delegate
					{
						if (settings.SelectedTeams.Contains(team)) settings.SelectedTeams.Remove(team);
						else
						{
							settings.SelectedTeams.Add(team);
							settings.AllyTeams.Remove(team);
						}
						settings.CustomTeam = settings.SelectedTeams.Count == 0 ? 0 : FirstTeam(settings.SelectedTeams);
						Commit();
						RefreshTeamsPageLater();
					};
					refreshSelected();
					Grid.SetColumn(selectedTeam, 2); row.Children.Add(selectedTeam);
					list.Children.Add(row);
				}
			}
			ScrollViewer scroll = new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
			Grid.SetRow(scroll, 1); layout.Children.Add(scroll);
			card.Child = layout;
			return card;
		}

		private UIElement ActorTableCard()
		{
			Border card = LargeCard(976, 420);
			Grid layout = new Grid();
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) });
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			Grid top = new Grid();
			top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
			top.Children.Add(new TextBlock { Text = "Объекты рядом", Foreground = Text, FontSize = 15, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
			actorTableFilter = new TextBox { Height = 29, Background = CardHoverBrush, Foreground = Text, BorderBrush = Border, Padding = new Thickness(9, 4, 9, 4), ToolTip = "Фильтр списка" };
			actorTableFilter.TextChanged += delegate { RefreshActorTable(); };
			Grid.SetColumn(actorTableFilter, 1); top.Children.Add(actorTableFilter);
			layout.Children.Add(top);
			layout.Children.Add(TableRow(new string[] { "ТИП", "НАЗВАНИЕ", "УР.", "ПЛЕМЯ", "ДИСТ.", "ВЫСОТА", "СОСТОЯНИЕ" }, new double[] { 72, 205, 48, 190, 75, 75, 225 }, true, 1));
			StackPanel actorRowsPanel = new StackPanel();
			actorRowPool = new RowPool(actorRowsPanel, new double[] { 72, 205, 48, 190, 75, 75, 225 });
			ScrollViewer scroll = new ScrollViewer { Content = actorRowsPanel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
			Grid.SetRow(scroll, 2); layout.Children.Add(scroll);
			card.Child = layout;
			RefreshActorTable();
			return card;
		}

		private UIElement TurretTableCard()
		{
			Border card = LargeCard(976, 430);
			Grid layout = new Grid();
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) });
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			layout.Children.Add(new TextBlock { Text = "Турели рядом", Foreground = Text, FontSize = 15, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
			layout.Children.Add(TableRow(new string[] { "ТУРЕЛЬ", "ПЛЕМЯ", "ПАТРОНЫ", "СОСТОЯНИЕ", "ДАЛЬН.", "AI", "ТРЕВОГА", "МЕТРЫ" }, new double[] { 190, 150, 105, 105, 80, 80, 105, 70 }, true, 1));
			StackPanel turretRowsPanel = new StackPanel();
			turretRowPool = new RowPool(turretRowsPanel, new double[] { 190, 150, 105, 105, 80, 80, 105, 70 });
			ScrollViewer scroll = new ScrollViewer { Content = turretRowsPanel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
			Grid.SetRow(scroll, 2); layout.Children.Add(scroll);
			card.Child = layout;
			RefreshTurretTable();
			return card;
		}

		private UIElement TurretModeNamesCard()
		{
			// The game's turret AI-targeting byte has no confirmed meaning in this
			// project — it could be a simple mode index or a bitmask of several
			// target-type toggles, and guessing wrong would show false information
			// about base defense. Instead of a built-in translation, the numbers
			// the tracker actually reads are listed here so the user can name each
			// one after checking the real setting on that turret in ARK.
			Border card = LargeCard(646, 300);
			Grid layout = new Grid();
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			StackPanel heading = new StackPanel();
			heading.Children.Add(new TextBlock { Text = "Названия режимов турели", Foreground = Text, FontSize = 14, FontWeight = FontWeights.SemiBold });
			heading.Children.Add(new TextBlock { Text = "Открой турель в игре, посмотри реальное название и впиши его сюда напротив того же числа из колонки «AI». Пустое поле — показывается как «Режим N».", Foreground = Muted, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 0) });
			layout.Children.Add(heading);
			Grid fields = new Grid();
			for (int column = 0; column < 5; column++) fields.ColumnDefinitions.Add(new ColumnDefinition());
			fields.RowDefinitions.Add(new RowDefinition());
			fields.RowDefinitions.Add(new RowDefinition());
			for (int value = 0; value < 10; value++)
			{
				int mode = value;
				string current;
				settings.TurretModeNames.TryGetValue(mode, out current);
				UIElement field = SmallField("Значение " + mode.ToString(CultureInfo.InvariantCulture), current ?? string.Empty, delegate(string text)
				{
					string trimmed = (text ?? string.Empty).Trim();
					if (trimmed.Length == 0) settings.TurretModeNames.Remove(mode);
					else settings.TurretModeNames[mode] = trimmed;
				});
				Grid.SetColumn(field, mode % 5);
				Grid.SetRow(field, mode / 5);
				fields.Children.Add(field);
			}
			Grid.SetRow(fields, 1);
			layout.Children.Add(fields);
			card.Child = layout;
			return card;
		}

		private UIElement NoglinStateCard()
		{
			Border card = LargeCard(646, 108);
			StackPanel panel = new StackPanel();
			panel.Children.Add(new TextBlock { Text = "Текущее состояние", Foreground = Text, FontSize = 14, FontWeight = FontWeights.SemiBold });
			noglinStatusText = new TextBlock { Text = currentNoglinStatus, Foreground = Live, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
			panel.Children.Add(noglinStatusText);
			card.Child = panel;
			return card;
		}

		private Border LargeCard(double width, double height)
		{
			return new Border { Width = width, Height = height, Margin = new Thickness(6), Background = CardBrush, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(16, 12, 16, 12) };
		}

		private UIElement TableRow(string[] cells, double[] widths, bool header, int rowIndex)
		{
			Grid row = new Grid { Height = header ? 31 : 38, Background = header ? Surface : (rowIndex % 2 == 0 ? CardHoverBrush : CardBrush), Margin = new Thickness(0, 0, 0, header ? 3 : 1) };
			for (int i = 0; i < widths.Length; i++) row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(widths[i]) });
			for (int i = 0; i < widths.Length; i++)
			{
				string value = i < cells.Length ? (cells[i] ?? string.Empty) : string.Empty;
				TextBlock text = new TextBlock { Text = value, Foreground = header ? Muted : Text, FontSize = header ? 10 : 11, FontWeight = header ? FontWeights.SemiBold : FontWeights.Normal, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(8, 0, 6, 0), ToolTip = value };
				Grid.SetColumn(text, i); row.Children.Add(text);
			}
			Grid.SetRow(row, rowIndex);
			return row;
		}

		private void RefreshActorTable()
		{
			if (actorRowPool == null) return;
			string filter = actorTableFilter == null ? string.Empty : (actorTableFilter.Text ?? string.Empty).Trim();
			List<string[]> filtered = actorRows;
			if (filter.Length > 0)
			{
				filtered = new List<string[]>();
				for (int i = 0; i < actorRows.Count && filtered.Count < 500; i++)
				{
					string[] row = actorRows[i];
					if (row.Any(delegate(string cell) { return (cell ?? string.Empty).IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0; })) filtered.Add(row);
				}
			}
			string empty = actorRows.Count == 0 ? "Ожидание первого сканирования мира…" : "По фильтру ничего не найдено.";
			actorRowPool.Render(filtered, 500, empty);
		}

		private void RefreshTurretTable()
		{
			if (turretRowPool == null) return;
			turretRowPool.Render(turretRows, 500, "В пределах выбранной дальности турели пока не найдены.");
		}

		private UIElement EventFeedCard()
		{
			Border card = LargeCard(976, 280);
			Grid layout = new Grid();
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			layout.Children.Add(new TextBlock { Text = "Живая лента событий", Foreground = Text, FontSize = 15, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
			StackPanel eventRowsPanel = new StackPanel();
			eventFeedPool = new EventFeedPool(eventRowsPanel);
			ScrollViewer scroll = new ScrollViewer { Content = eventRowsPanel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
			Grid.SetRow(scroll, 1); layout.Children.Add(scroll);
			card.Child = layout;
			RefreshEventFeed();
			return card;
		}

		private void RefreshEventFeed()
		{
			if (eventFeedPool == null) return;
			eventFeedPool.Render(eventRows, 24, "Событий пока нет. Появятся здесь по мере игры — тот же список, что и во всплывающих уведомлениях поверх игры.");
		}

		private UIElement StructureClassListCard()
		{
			Border card = LargeCard(976, 430);
			Grid layout = new Grid();
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			Grid top = new Grid();
			top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
			top.Children.Add(new TextBlock { Text = "Типы построек и ручные группы", Foreground = Text, FontSize = 15, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
			layout.Children.Add(top);

			string[] values = getKnownStructureClasses == null ? new string[0] : getKnownStructureClasses();
			if (values == null) values = new string[0];
			TextBox search = new TextBox { Width = 360, Height = 29, Background = CardHoverBrush, Foreground = Text, BorderBrush = Border, Padding = new Thickness(9, 4, 9, 4), ToolTip = "Поиск по названию или внутреннему классу", HorizontalAlignment = HorizontalAlignment.Left };
			Button selectAll = new Button { Content = "ПОКАЗЫВАТЬ ВСЕ", Width = 150, Height = 29, Margin = new Thickness(372, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
			Button selectNone = new Button { Content = "ВЫБРАТЬ ВРУЧНУЮ", Width = 168, Height = 29, Margin = new Thickness(530, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
			Button clearAll = new Button { Content = "СНЯТЬ ВСЕ", Width = 130, Height = 29, Margin = new Thickness(706, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
			ConfigureButton(selectAll, 150, 29, true); ConfigureButton(selectNone, 168, 29, false); ConfigureButton(clearAll, 130, 29, false);
			selectNone.IsEnabled = values.Length > 0;
			clearAll.IsEnabled = values.Length > 0;
			Grid tools = new Grid(); tools.Children.Add(search); tools.Children.Add(selectAll); tools.Children.Add(selectNone); tools.Children.Add(clearAll);
			Grid.SetRow(tools, 1); layout.Children.Add(tools);

			ListBox list = new ListBox { Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = Text };
			string selectedClassName = null;
			Button assignment = new Button { Width = 270, Height = 29, HorizontalAlignment = HorizontalAlignment.Left, IsEnabled = false };
			ConfigureButton(assignment, 270, 29, false);
			TextBlock assignmentHint = new TextBlock { Text = "Выберите строку ниже, затем назначьте ей группу", Foreground = Muted, FontSize = 11, Margin = new Thickness(284, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
			Grid assignmentRow = new Grid(); assignmentRow.Children.Add(assignment); assignmentRow.Children.Add(assignmentHint);
			Grid.SetRow(assignmentRow, 2); layout.Children.Add(assignmentRow);

			Popup assignmentPopup = new Popup { Placement = PlacementMode.Bottom, PlacementTarget = assignment, StaysOpen = false, AllowsTransparency = true };
			StackPanel assignmentOptions = new StackPanel();
			Border assignmentBorder = new Border { Background = CardBrush, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Padding = new Thickness(5), Width = 270, Child = assignmentOptions };
			assignmentPopup.Child = assignmentBorder;
			Action refreshAssignment = delegate
			{
				assignment.IsEnabled = !string.IsNullOrWhiteSpace(selectedClassName);
				string categoryId;
				StructureCategoryRule category = null;
				if (!string.IsNullOrWhiteSpace(selectedClassName) && settings.StructureClassCategories.TryGetValue(selectedClassName, out categoryId)) category = FindCategory(categoryId);
				assignment.Content = category == null ? "Группа: автоматически   ▾" : "Группа: " + category.Name + "   ▾";
			};
			Action rebuild = null;

			Button categoryFilter = new Button { HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(9, 0, 8, 0) };
			ConfigureButton(categoryFilter, 220, 29, false);
			Popup categoryFilterPopup = new Popup { Placement = PlacementMode.Bottom, PlacementTarget = categoryFilter, StaysOpen = false, AllowsTransparency = true };
			StackPanel categoryFilterOptions = new StackPanel();
			Border categoryFilterBorder = new Border { Background = CardBrush, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Padding = new Thickness(5), Width = 220, Child = categoryFilterOptions };
			categoryFilterPopup.Child = categoryFilterBorder;
			Action refreshCategoryFilter = delegate
			{
				StructureCategoryRule filterCategory = string.IsNullOrWhiteSpace(structureClassCategoryFilter) ? null : FindCategory(structureClassCategoryFilter);
				categoryFilter.Content = "Категория: " + (filterCategory == null ? "Все" : filterCategory.Name) + "   ▾";
			};
			Action<string, string> addCategoryFilterOption = delegate(string id, string name)
			{
				Button option = new Button { Content = name, Height = 32, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(10, 0, 8, 0), Margin = new Thickness(0, 1, 0, 1) };
				ConfigureButton(option, 0, 32, false);
				option.Click += delegate
				{
					structureClassCategoryFilter = string.IsNullOrWhiteSpace(id) ? null : id;
					categoryFilterPopup.IsOpen = false;
					refreshCategoryFilter();
					if (rebuild != null) rebuild();
				};
				categoryFilterOptions.Children.Add(option);
			};
			addCategoryFilterOption(string.Empty, "Все категории");
			for (int i = 0; i < settings.StructureCategories.Count; i++) addCategoryFilterOption(settings.StructureCategories[i].Id, settings.StructureCategories[i].Name);
			categoryFilter.Click += delegate { categoryFilterPopup.IsOpen = !categoryFilterPopup.IsOpen; };
			refreshCategoryFilter();
			Grid.SetColumn(categoryFilter, 1); top.Children.Add(categoryFilter);
			top.Children.Add(categoryFilterPopup);

			Action<string, string> addAssignment = delegate(string id, string name)
			{
				Button option = new Button { Content = name, Height = 32, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(10, 0, 8, 0), Margin = new Thickness(0, 1, 0, 1) };
				ConfigureButton(option, 0, 32, false);
				option.Click += delegate
				{
					if (string.IsNullOrWhiteSpace(selectedClassName)) return;
					if (string.IsNullOrWhiteSpace(id)) settings.StructureClassCategories.Remove(selectedClassName);
					else settings.StructureClassCategories[selectedClassName] = id;
					assignmentPopup.IsOpen = false; Commit(); refreshAssignment(); if (rebuild != null) rebuild();
				};
				assignmentOptions.Children.Add(option);
			};
			addAssignment(string.Empty, "Автоматически по правилам");
			for (int i = 0; i < settings.StructureCategories.Count; i++) addAssignment(settings.StructureCategories[i].Id, settings.StructureCategories[i].Name);
			assignment.Click += delegate { assignmentPopup.IsOpen = !assignmentPopup.IsOpen; };
			assignmentRow.Children.Add(assignmentPopup);

			rebuild = delegate
			{
				list.Items.Clear();
				string query = (search.Text ?? string.Empty).Trim();
				for (int i = 0; i < values.Length; i++)
				{
					string[] parts = values[i].Split(new char[] { '\t' }, 2);
					string display = parts.Length > 0 ? parts[0] : values[i];
					string className = parts.Length > 1 ? parts[1] : values[i];
					if (query.Length > 0 && display.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) < 0 && className.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
					// The resolved category (auto keyword match or manual override) is what
					// actually decides visibility/grouping, unlike the override-only lookup
					// used by the assignment popup below - showing it here is what makes the
					// "which classes are in this category" filter and count meaningful.
					StructureCategoryRule resolvedCategory = settings.FindStructureCategory(new ActorRecord { Kind = ActorKind.Structure, ClassName = className, DisplayName = display });
					if (!string.IsNullOrWhiteSpace(structureClassCategoryFilter) &&
						(resolvedCategory == null || !string.Equals(resolvedCategory.Id, structureClassCategoryFilter, StringComparison.OrdinalIgnoreCase))) continue;
					bool hasManualOverride = settings.StructureClassCategories.ContainsKey(className);
					Grid row = new Grid { Height = 38 };
					row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
					row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
					row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
					row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
					CheckBox box = new CheckBox { Tag = className, IsChecked = settings.AllStructureClasses || settings.StructureClasses.Contains(className), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
					TextBlock name = new TextBlock { Text = display, Foreground = Text, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = className };
					TextBlock group = new TextBlock { Text = resolvedCategory == null ? "—" : resolvedCategory.Name, Foreground = hasManualOverride ? Live : Muted, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0), TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = hasManualOverride ? "Назначено вручную" : "Определено автоматически по правилам" };
					Grid.SetColumn(name, 1); Grid.SetColumn(group, 2); row.Children.Add(box); row.Children.Add(name); row.Children.Add(group);

					// "Показывать отдельно" is independent from the category system: pinned
					// classes always render, standalone (never clustered), even when their
					// whole category is switched off - built for things like S+ Sensor,
					// obelisks or loot crates that don't belong to any tribe-base category.
					Button pin = new Button { Height = 27, HorizontalContentAlignment = HorizontalAlignment.Center, ToolTip = "Всегда показывать эту постройку отдельно, даже если её категория выключена" };
					ConfigureButton(pin, 106, 27, false);
					Action refreshPin = delegate
					{
						bool pinned = settings.PinnedStructureClasses.Contains(className);
						pin.Content = pinned ? "Отдельно ✓" : "Отдельно";
						pin.Background = pinned ? Accent : CardHoverBrush;
						pin.Foreground = pinned ? Brushes.White : Muted;
					};
					pin.Click += delegate
					{
						if (settings.PinnedStructureClasses.Contains(className)) settings.PinnedStructureClasses.Remove(className);
						else settings.PinnedStructureClasses.Add(className);
						Commit();
						refreshPin();
					};
					refreshPin();
					Grid.SetColumn(pin, 3); row.Children.Add(pin);

					ListBoxItem item = new ListBoxItem { Content = row, Tag = className, Background = string.Equals(selectedClassName, className, StringComparison.OrdinalIgnoreCase) ? AccentSoft : Brushes.Transparent, Foreground = Text, Padding = new Thickness(0) };
					box.PreviewMouseLeftButtonDown += delegate { list.SelectedItem = item; };
					box.Checked += delegate(object sender, RoutedEventArgs args) { SetStructureClass((CheckBox)sender, true, values); };
					box.Unchecked += delegate(object sender, RoutedEventArgs args) { SetStructureClass((CheckBox)sender, false, values); };
					list.Items.Add(item);
					if (string.Equals(selectedClassName, className, StringComparison.OrdinalIgnoreCase)) list.SelectedItem = item;
				}
				if (list.Items.Count == 0) list.Items.Add(new ListBoxItem { Content = values.Length == 0 ? "Список заполнится после первого сканирования мира." : "По фильтру ничего не найдено.", Foreground = Muted, IsEnabled = false, Padding = new Thickness(8, 12, 0, 0) });
				refreshAssignment();
			};
			list.SelectionChanged += delegate
			{
				ListBoxItem selected = list.SelectedItem as ListBoxItem;
				if (selected != null && selected.Tag is string) selectedClassName = (string)selected.Tag;
				refreshAssignment();
			};
			search.TextChanged += delegate { rebuild(); };
			selectAll.Click += delegate
			{
				settings.ShowStructures = true;
				settings.AllStructureClasses = true;
				settings.StructureClasses.Clear();
				Commit();
				ShowPage("Постройки");
			};
			selectNone.Click += delegate
			{
				settings.ShowStructures = true;
				SetAllStructureClasses(false);
				Commit();
				ShowPage("Постройки");
			};
			clearAll.Click += delegate
			{
				// Unlike "ВЫБРАТЬ ВРУЧНУЮ" (which starts from everything checked so a
				// single click never makes the base disappear), this starts from an
				// empty selection so picking just a few classes doesn't mean
				// unchecking hundreds of boxes one by one first.
				settings.AllStructureClasses = false;
				settings.StructureClasses.Clear();
				Commit();
				ShowPage("Постройки");
			};
			rebuild();
			ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = list };
			Grid.SetRow(scroll, 3); layout.Children.Add(scroll);
			card.Child = layout;
			return card;
		}

		private void SetStructureClass(CheckBox box, bool selected, string[] allValues)
		{
			if (box == null || !(box.Tag is string)) return;
			if (settings.AllStructureClasses)
			{
				settings.AllStructureClasses = false;
				settings.StructureClasses.Clear();
				for (int i = 0; i < allValues.Length; i++)
				{
					string[] parts = allValues[i].Split(new char[] { '\t' }, 2);
					settings.StructureClasses.Add(parts.Length > 1 ? parts[1] : allValues[i]);
				}
			}
			string className = (string)box.Tag;
			if (selected) settings.StructureClasses.Add(className);
			else settings.StructureClasses.Remove(className);
			Commit();
		}

		private void SetAllStructureClasses(bool value)
		{
			settings.AllStructureClasses = value;
			if (value)
			{
				settings.StructureClasses.Clear();
				return;
			}
			// Switching from "all" to a selective filter must start with the
			// currently known classes selected. Otherwise a single toggle hides all
			// structures and gives the impression that ESP stopped working.
			if (settings.StructureClasses.Count != 0 || getKnownStructureClasses == null) return;
			string[] values = getKnownStructureClasses() ?? new string[0];
			if (values.Length == 0)
			{
				settings.AllStructureClasses = true;
				return;
			}
			for (int i = 0; i < values.Length; i++)
			{
				string[] parts = values[i].Split(new char[] { '\t' }, 2);
				settings.StructureClasses.Add(parts.Length > 1 ? parts[1] : values[i]);
			}
		}

		private void SetAllCategoryGrouping(bool value)
		{
			settings.GroupStructures = value;
			for (int i = 0; i < settings.StructureCategories.Count; i++) settings.StructureCategories[i].Group = value;
		}

		private void SetAllCategoryLabels(int value)
		{
			settings.StructureLabelMode = Math.Max(0, Math.Min(2, value));
			bool showNames = settings.StructureLabelMode != 0;
			bool group = settings.StructureLabelMode != 2;
			settings.GroupStructures = group;
			for (int i = 0; i < settings.StructureCategories.Count; i++)
			{
				settings.StructureCategories[i].ShowNames = showNames;
				settings.StructureCategories[i].Group = group;
			}
		}

		private void SetAllCategoryDistance(double metres)
		{
			float distanceCm = (float)Math.Max(1.0, Math.Min(100.0, metres)) * 100f;
			settings.StructureGroupingDistanceCm = distanceCm;
			for (int i = 0; i < settings.StructureCategories.Count; i++) settings.StructureCategories[i].GroupingDistanceCm = distanceCm;
		}

		private UIElement CategoryCard(StructureCategoryRule category)
		{
			Border card = LargeCard(646, 348);
			Grid layout = new Grid();
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(34) });
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(68) });
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
			layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });
			TextBlock title = new TextBlock { Text = category.Name, Foreground = Text, FontSize = 14, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
			layout.Children.Add(title);
			Button enabled = new Button { Width = 53, Height = 25, HorizontalAlignment = HorizontalAlignment.Right };
			ConfigureButton(enabled, 53, 25, false);
			Action refreshEnabled = delegate { enabled.Content = category.Enabled ? "ВКЛ" : "ВЫКЛ"; enabled.Background = category.Enabled ? Accent : CardHoverBrush; enabled.Foreground = category.Enabled ? Brushes.White : Muted; };
			enabled.Click += delegate { category.Enabled = !category.Enabled; refreshEnabled(); Commit(); };
			refreshEnabled();
			layout.Children.Add(enabled);
			Grid modes = new Grid();
			modes.ColumnDefinitions.Add(new ColumnDefinition()); modes.ColumnDefinitions.Add(new ColumnDefinition());
			modes.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) }); modes.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
			Button grouping = new Button { Height = 28, Margin = new Thickness(0, 0, 3, 4) };
			ConfigureButton(grouping, 0, 28, false);
			grouping.Content = category.Group ? "Группировать   ▾" : "Каждый элемент   ▾";
			grouping.Click += delegate { category.Group = !category.Group; grouping.Content = category.Group ? "Группировать   ▾" : "Каждый элемент   ▾"; Commit(); };
			modes.Children.Add(grouping);
			Button names = new Button { Height = 28, Margin = new Thickness(3, 0, 0, 4) };
			ConfigureButton(names, 0, 28, false);
			names.Content = category.ShowNames ? "Подписи: показывать" : "Подписи: только точки";
			names.Click += delegate { category.ShowNames = !category.ShowNames; names.Content = category.ShowNames ? "Подписи: показывать" : "Подписи: только точки"; Commit(); };
			Grid.SetColumn(names, 1); modes.Children.Add(names);
			Button tribe = new Button { Height = 28, Margin = new Thickness(0, 0, 3, 0) };
			ConfigureButton(tribe, 0, 28, false);
			tribe.Content = category.ShowTribe ? "Трайб: показывать" : "Трайб: скрыт";
			tribe.Click += delegate { category.ShowTribe = !category.ShowTribe; tribe.Content = category.ShowTribe ? "Трайб: показывать" : "Трайб: скрыт"; Commit(); };
			Grid.SetRow(tribe, 1); modes.Children.Add(tribe);
			Button own = new Button { Height = 28, Margin = new Thickness(3, 0, 0, 0) };
			ConfigureButton(own, 0, 28, false);
			own.Content = category.ShowOwn ? "Свои: показывать" : "Свои: скрыты";
			own.Click += delegate { category.ShowOwn = !category.ShowOwn; own.Content = category.ShowOwn ? "Свои: показывать" : "Свои: скрыты"; Commit(); };
			Grid.SetRow(own, 1); Grid.SetColumn(own, 1); modes.Children.Add(own);
			Grid.SetRow(modes, 1); layout.Children.Add(modes);

			Grid metrics = new Grid(); for (int i = 0; i < 4; i++) metrics.ColumnDefinitions.Add(new ColumnDefinition());
			metrics.Children.Add(SmallField("Группа, м", (category.GroupingDistanceCm / 100f).ToString("0.#", CultureInfo.InvariantCulture), delegate(string text) { double value; if (TryNumber(text, out value)) category.GroupingDistanceCm = (float)Math.Max(1, Math.Min(500, value)) * 100f; }));
			UIElement nameDistance = SmallField("Подпись до, м", (category.NameDistanceCm / 100f).ToString("0.#", CultureInfo.InvariantCulture), delegate(string text) { double value; if (TryNumber(text, out value)) category.NameDistanceCm = (float)Math.Max(0, Math.Min(50000, value)) * 100f; }); Grid.SetColumn(nameDistance, 1); metrics.Children.Add(nameDistance);
			UIElement limit = SmallField("Лимит подписей", category.MaxLabels.ToString(CultureInfo.InvariantCulture), delegate(string text) { int value; if (int.TryParse(text, out value)) category.MaxLabels = Math.Max(0, Math.Min(1000, value)); }); Grid.SetColumn(limit, 2); metrics.Children.Add(limit);
			UIElement priority = SmallField("Приоритет", category.Priority.ToString(CultureInfo.InvariantCulture), delegate(string text) { int value; if (int.TryParse(text, out value)) category.Priority = Math.Max(0, Math.Min(1000, value)); }); Grid.SetColumn(priority, 3); metrics.Children.Add(priority);
			Grid.SetRow(metrics, 2); layout.Children.Add(metrics);

			Grid rules = new Grid(); rules.ColumnDefinitions.Add(new ColumnDefinition()); rules.ColumnDefinitions.Add(new ColumnDefinition());
			bool fallback = string.Equals(category.Id, "other", StringComparison.OrdinalIgnoreCase);
			rules.Children.Add(SmallField("Включать слова (через |)", fallback ? "Все прочие" : string.Join("|", category.Includes.ToArray()), delegate(string text) { if (!fallback) category.Includes = StructureCategoryRule.SplitKeywords(text).ToList(); }, fallback));
			UIElement excludes = SmallField("Исключать слова (через |)", string.Join("|", category.Excludes.ToArray()), delegate(string text) { category.Excludes = StructureCategoryRule.SplitKeywords(text).ToList(); }); Grid.SetColumn(excludes, 1); rules.Children.Add(excludes);
			Grid.SetRow(rules, 3); layout.Children.Add(rules);

			Grid colorRow = new Grid(); colorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) }); colorRow.ColumnDefinitions.Add(new ColumnDefinition());
			UIElement colorPicker = ColorPicker(delegate { return category.Color; }, delegate(int value) { category.Color = value; }, 108);
			colorRow.Children.Add(colorPicker); TextBlock hint = new TextBlock { Text = "Цвет точек и подписей группы", Foreground = Muted, FontSize = 11, VerticalAlignment = VerticalAlignment.Center }; Grid.SetColumn(hint, 1); colorRow.Children.Add(hint);
			Grid.SetRow(colorRow, 4); layout.Children.Add(colorRow);

			// Jumps to the flat class list pre-filtered to just this category, so
			// "add/remove structures to a category" happens in the same place instead
			// of duplicating a whole second list-rendering UI inside every card.
			string[] allKnownClasses = getKnownStructureClasses == null ? new string[0] : (getKnownStructureClasses() ?? new string[0]);
			int classCount = 0;
			for (int i = 0; i < allKnownClasses.Length; i++)
			{
				string[] parts = allKnownClasses[i].Split(new char[] { '\t' }, 2);
				string display = parts.Length > 0 ? parts[0] : allKnownClasses[i];
				string className = parts.Length > 1 ? parts[1] : allKnownClasses[i];
				StructureCategoryRule resolved = settings.FindStructureCategory(new ActorRecord { Kind = ActorKind.Structure, ClassName = className, DisplayName = display });
				if (resolved != null && string.Equals(resolved.Id, category.Id, StringComparison.OrdinalIgnoreCase)) classCount++;
			}
			Button classesButton = new Button { HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(10, 0, 8, 0) };
			ConfigureButton(classesButton, 260, 30, false);
			classesButton.Content = "Классы в категории (" + classCount.ToString(CultureInfo.InvariantCulture) + ")   →";
			classesButton.Click += delegate { structureClassCategoryFilter = category.Id; ShowPage("Постройки"); };
			Grid classesRow = new Grid(); classesRow.Children.Add(classesButton);
			Grid.SetRow(classesRow, 5); layout.Children.Add(classesRow);

			card.Child = layout;
			return card;
		}

		private StructureCategoryRule FindCategory(string id)
		{
			for (int i = 0; i < settings.StructureCategories.Count; i++)
				if (string.Equals(settings.StructureCategories[i].Id, id, StringComparison.OrdinalIgnoreCase)) return settings.StructureCategories[i];
			return null;
		}

		private UIElement SmallField(string label, string value, Action<string> save)
		{
			return SmallField(label, value, save, false);
		}

		private UIElement SmallField(string label, string value, Action<string> save, bool readOnly)
		{
			StackPanel panel = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
			panel.Children.Add(new TextBlock { Text = label, Foreground = Muted, FontSize = 10, TextTrimming = TextTrimming.CharacterEllipsis });
			TextBox input = new TextBox { Height = 27, Text = value ?? string.Empty, Foreground = readOnly ? Muted : Text, Background = CardHoverBrush, BorderBrush = Border, Padding = new Thickness(7, 3, 7, 3), IsReadOnly = readOnly, Margin = new Thickness(0, 3, 0, 0) };
			if (!readOnly)
			{
				input.LostKeyboardFocus += delegate { if (save != null) save(input.Text); Commit(); };
				input.KeyDown += delegate(object sender, KeyEventArgs args) { if (args.Key == Key.Enter) Keyboard.ClearFocus(); };
			}
			panel.Children.Add(input);
			return panel;
		}

		private static bool TryNumber(string text, out double value)
		{
			return double.TryParse((text ?? string.Empty).Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
		}

		private static int TeamModeToChoice(TeamFilterMode mode)
		{
			if (mode == TeamFilterMode.Enemies) return 1;
			if (mode == TeamFilterMode.Mine) return 2;
			if (mode == TeamFilterMode.Custom) return 3;
			return 0;
		}

		private static TeamFilterMode ChoiceToTeamMode(int choice)
		{
			if (choice == 1) return TeamFilterMode.Enemies;
			if (choice == 2) return TeamFilterMode.Mine;
			if (choice == 3) return TeamFilterMode.Custom;
			return TeamFilterMode.All;
		}

		private void RunOnUi(Action action)
		{
			if (action == null) return;
			if (Dispatcher.CheckAccess()) action();
			else Dispatcher.BeginInvoke(action);
		}

		private static bool TryReadColor(string text, out int color)
		{
			text = (text ?? string.Empty).Trim().TrimStart('#');
			color = 0;
			return text.Length == 6 && int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out color);
		}

		private static Brush ColorBrush(int color)
		{
			byte red = (byte)((color >> 16) & 0xFF);
			byte green = (byte)((color >> 8) & 0xFF);
			byte blue = (byte)(color & 0xFF);
			return new SolidColorBrush(Color.FromRgb(red, green, blue));
		}

		private static string JoinTeams(HashSet<int> teams)
		{
			if (teams == null || teams.Count == 0) return string.Empty;
			List<int> values = new List<int>(teams); values.Sort();
			return string.Join(", ", values.ConvertAll(delegate(int value) { return value.ToString(CultureInfo.InvariantCulture); }).ToArray());
		}

		private static HashSet<int> ReadTeams(string value)
		{
			HashSet<int> result = new HashSet<int>();
			foreach (string part in (value ?? string.Empty).Split(new char[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
			{
				int team;
				if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out team) && team != 0) result.Add(team);
			}
			return result;
		}

		private static int FirstTeam(HashSet<int> teams)
		{
			foreach (int team in teams) return team;
			return 0;
		}

		private void SetManualAllies(HashSet<int> value)
		{
			settings.AllyTeams = value ?? new HashSet<int>();
			settings.SelectedTeams.ExceptWith(settings.AllyTeams);
			settings.AllyTeams.Remove(localTribe);
			settings.CustomTeam = settings.SelectedTeams.Count == 0 ? 0 : FirstTeam(settings.SelectedTeams);
			RefreshTeamsPageLater();
		}

		private void SetManualSelectedTeams(HashSet<int> value)
		{
			settings.SelectedTeams = value ?? new HashSet<int>();
			settings.AllyTeams.ExceptWith(settings.SelectedTeams);
			settings.CustomTeam = settings.SelectedTeams.Count == 0 ? 0 : FirstTeam(settings.SelectedTeams);
			RefreshTeamsPageLater();
		}

		private void RefreshTeamsPageLater()
		{
			Dispatcher.BeginInvoke(new Action(delegate
			{
				if (string.Equals(currentPage, "Племена и цвета", StringComparison.Ordinal)) ShowPage(currentPage);
			}));
		}

		private void ConfigureButton(Button button, double width, double height, bool primary)
		{
			if (width > 0) button.Width = width;
			if (height > 0) button.Height = height;
			button.Background = primary ? Accent : Surface;
			button.Foreground = primary ? Brushes.White : Muted;
			button.BorderBrush = primary ? Accent : Brushes.Transparent;
			button.BorderThickness = new Thickness(0);
			button.Template = RoundedButtonTemplate;
			button.FocusVisualStyle = null;
			button.Cursor = System.Windows.Input.Cursors.Hand;
		}

		private void UpdateEspButton()
		{
			espButton.Content = settings.OverlayEnabled ? (menuOpen ? "ESP ВКЛ · INSERT" : "ESP  ВКЛ") : "ESP  ВЫКЛ";
			espButton.Background = settings.OverlayEnabled ? Accent : CardHoverBrush;
			espButton.Foreground = settings.OverlayEnabled ? Brushes.White : Muted;
		}

		private void Commit()
		{
			settings.Revision++;
			apply();
			UpdateEspButton();
		}

		private static Brush Brush(string value)
		{
			return new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
		}

		// The old WinForms DataGridView only ever painted the rows actually on
		// screen; clearing/re-adding its Rows collection was cheap. A plain WPF
		// StackPanel has no such virtualization, so clearing it and rebuilding a
		// full Grid+TextBlock visual tree for up to 500 rows several times a
		// second (actor/turret tables refresh on live snapshot data) produced a
		// real, visible stutter that did not exist before the WPF migration.
		// This pool keeps the row elements alive across refreshes and only
		// touches the Text of a cell when its value actually changed.
		private sealed class RowPool
		{
			private readonly StackPanel host;
			private readonly double[] widths;
			private readonly List<Grid> rows = new List<Grid>();
			private readonly List<TextBlock[]> cells = new List<TextBlock[]>();
			private TextBlock emptyText;

			internal RowPool(StackPanel panel, double[] columnWidths)
			{
				host = panel;
				widths = columnWidths;
			}

			internal void Render(IList<string[]> data, int limit, string emptyMessage)
			{
				int count = Math.Min(data == null ? 0 : data.Count, limit);
				while (rows.Count < count) CreateRow();
				for (int i = 0; i < count; i++)
				{
					SetRow(i, data[i]);
					rows[i].Visibility = Visibility.Visible;
				}
				for (int i = count; i < rows.Count; i++) rows[i].Visibility = Visibility.Collapsed;
				if (count == 0)
				{
					if (emptyText == null)
					{
						emptyText = new TextBlock { Foreground = Muted, FontSize = 12, Margin = new Thickness(8, 16, 0, 0) };
						host.Children.Add(emptyText);
					}
					emptyText.Text = emptyMessage;
					emptyText.Visibility = Visibility.Visible;
				}
				else if (emptyText != null)
				{
					emptyText.Visibility = Visibility.Collapsed;
				}
			}

			private void CreateRow()
			{
				// Background alternation only depends on the row's position in the
				// pool, which never changes once a row is created, so it is set once
				// here instead of on every Render() call.
				Grid row = new Grid { Height = 38, Margin = new Thickness(0, 0, 0, 1), Background = rows.Count % 2 == 0 ? CardHoverBrush : CardBrush };
				for (int i = 0; i < widths.Length; i++) row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(widths[i]) });
				TextBlock[] rowCells = new TextBlock[widths.Length];
				for (int i = 0; i < widths.Length; i++)
				{
					TextBlock text = new TextBlock { Foreground = Text, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(8, 0, 6, 0) };
					Grid.SetColumn(text, i);
					row.Children.Add(text);
					rowCells[i] = text;
				}
				rows.Add(row);
				cells.Add(rowCells);
				host.Children.Add(row);
			}

			private void SetRow(int index, string[] values)
			{
				TextBlock[] rowCells = cells[index];
				for (int i = 0; i < rowCells.Length; i++)
				{
					string value = values != null && i < values.Length ? (values[i] ?? string.Empty) : string.Empty;
					TextBlock cell = rowCells[i];
					if (!string.Equals(cell.Text, value, StringComparison.Ordinal))
					{
						cell.Text = value;
						cell.ToolTip = value;
					}
				}
			}
		}

		// Same reuse-instead-of-rebuild approach as RowPool, but for the event
		// feed's stacked title/text/age cards instead of a fixed-column table.
		private sealed class EventFeedPool
		{
			private readonly StackPanel host;
			private readonly List<Border> rows = new List<Border>();
			private readonly List<TextBlock> titles = new List<TextBlock>();
			private readonly List<TextBlock> texts = new List<TextBlock>();
			private readonly List<TextBlock> ages = new List<TextBlock>();
			private TextBlock emptyText;

			internal EventFeedPool(StackPanel panel)
			{
				host = panel;
			}

			internal void Render(IList<string[]> data, int limit, string emptyMessage)
			{
				int count = Math.Min(data == null ? 0 : data.Count, limit);
				while (rows.Count < count) CreateRow();
				for (int i = 0; i < count; i++)
				{
					SetRow(i, data[i]);
					rows[i].Visibility = Visibility.Visible;
				}
				for (int i = count; i < rows.Count; i++) rows[i].Visibility = Visibility.Collapsed;
				if (count == 0)
				{
					if (emptyText == null)
					{
						emptyText = new TextBlock { Foreground = Muted, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 16, 0, 0) };
						host.Children.Add(emptyText);
					}
					emptyText.Text = emptyMessage;
					emptyText.Visibility = Visibility.Visible;
				}
				else if (emptyText != null)
				{
					emptyText.Visibility = Visibility.Collapsed;
				}
			}

			private void CreateRow()
			{
				Border border = new Border { Background = CardHoverBrush, CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 0, 6), Padding = new Thickness(12, 8, 12, 8), BorderThickness = new Thickness(3, 0, 0, 0) };
				Grid grid = new Grid();
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
				grid.RowDefinitions.Add(new RowDefinition());
				grid.RowDefinitions.Add(new RowDefinition());
				TextBlock title = new TextBlock { FontSize = 12, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
				TextBlock text = new TextBlock { Foreground = Muted, FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 2, 0, 0) };
				TextBlock age = new TextBlock { Foreground = Muted, FontSize = 10, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(8, 0, 0, 0) };
				Grid.SetRow(title, 0); Grid.SetColumn(title, 0); grid.Children.Add(title);
				Grid.SetRow(text, 1); Grid.SetColumn(text, 0); grid.Children.Add(text);
				Grid.SetRow(age, 0); Grid.SetColumn(age, 1); grid.Children.Add(age);
				border.Child = grid;
				rows.Add(border);
				titles.Add(title);
				texts.Add(text);
				ages.Add(age);
				host.Children.Add(border);
			}

			private void SetRow(int index, string[] values)
			{
				string title = values != null && values.Length > 0 ? (values[0] ?? string.Empty) : string.Empty;
				string text = values != null && values.Length > 1 ? (values[1] ?? string.Empty) : string.Empty;
				string age = values != null && values.Length > 2 ? (values[2] ?? string.Empty) : string.Empty;
				string severity = values != null && values.Length > 3 ? (values[3] ?? string.Empty) : string.Empty;
				if (!string.Equals(titles[index].Text, title, StringComparison.Ordinal)) titles[index].Text = title;
				if (!string.Equals(texts[index].Text, text, StringComparison.Ordinal)) texts[index].Text = text;
				if (!string.Equals(ages[index].Text, age, StringComparison.Ordinal)) ages[index].Text = age;
				Brush accent = SeverityBrush(severity);
				titles[index].Foreground = accent;
				rows[index].BorderBrush = accent;
			}

			private static Brush SeverityBrush(string severity)
			{
				if (string.Equals(severity, "Danger", StringComparison.OrdinalIgnoreCase)) return Brush("#FF4F61");
				if (string.Equals(severity, "Warning", StringComparison.OrdinalIgnoreCase)) return Brush("#FFC857");
				if (string.Equals(severity, "Good", StringComparison.OrdinalIgnoreCase)) return Brush("#4BE69D");
				return Muted;
			}
		}
	}
}
