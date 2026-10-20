using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace BirdsLibrary
{
    public sealed class Game
    {
        public string id { get; set; }
        public string name { get; set; }
        public string label { get; set; }
        public string description { get; set; }
        public string year { get; set; }
        public string accent { get; set; }
        public string link { get; set; }
        public string image { get; set; }
        public string group { get; set; }
        public string owner { get; set; }
        public string sourceUrl { get; set; }
        public string availability { get; set; }
    }

    public sealed class UserLibrary
    {
        public List<string> enabled { get; set; }
        public Dictionary<string, string> paths { get; set; }
        public UserLibrary() { enabled = new List<string>(); paths = new Dictionary<string, string>(); }
    }

    public static class LibraryStore
    {
        public static bool IsLaunchPath(string path)
        {
            if (String.IsNullOrWhiteSpace(path) || !System.IO.Path.IsPathRooted(path)) return false;
            var extension = System.IO.Path.GetExtension(path);
            return extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) || extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase);
        }
        public static UserLibrary Load(string path, List<Game> catalog)
        {
            if (!File.Exists(path)) return new UserLibrary { enabled = catalog.Select(g => g.id).ToList() };
            var value = new JavaScriptSerializer().Deserialize<UserLibrary>(File.ReadAllText(path));
            if (value == null || value.enabled == null || value.paths == null) throw new InvalidDataException("Configurazione incompleta.");
            var ids = new HashSet<string>(catalog.Select(g => g.id));
            value.enabled = value.enabled.Where(ids.Contains).Distinct().ToList();
            value.paths = value.paths.Where(p => ids.Contains(p.Key) && IsLaunchPath(p.Value)).ToDictionary(p => p.Key, p => p.Value);
            return value;
        }
        public static void Save(string path, UserLibrary library)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, new JavaScriptSerializer().Serialize(library));
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
        }
    }

    public sealed class LibraryWindow : Window
    {
        private readonly string root = AppDomain.CurrentDomain.BaseDirectory;
        private readonly TextBlock status = new TextBlock();
        private List<Game> games;
        private readonly string settingsPath;
        private UserLibrary library;
        private readonly bool previewMode;
        private string section = "home";
        private readonly Dictionary<string, BitmapImage> images = new Dictionary<string, BitmapImage>();
        private StackPanel page;
        private ScrollViewer scroll;
        private readonly Dictionary<string, Button> navigation = new Dictionary<string, Button>();

        public LibraryWindow(string dataDirectory = null, bool preview = false)
        {
            previewMode = preview;
            settingsPath = System.IO.Path.Combine(dataDirectory ?? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NostalgiaGameHub"), "library.json");
            Title = "Game Hub · La tua libreria";
            Width = Math.Min(1180, SystemParameters.WorkArea.Width * 0.92);
            Height = Math.Min(820, SystemParameters.WorkArea.Height * 0.94);
            MinWidth = 800;
            MinHeight = 540;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResize;
            System.Windows.Shell.WindowChrome.SetWindowChrome(this, new System.Windows.Shell.WindowChrome
            {
                CaptionHeight = 0,
                ResizeBorderThickness = new Thickness(6),
                GlassFrameThickness = new Thickness(0),
                CornerRadius = new CornerRadius(10),
                UseAeroCaptionButtons = false
            });
            Background = Brush("#101219");
            Foreground = Brush("#F5F6FA");
            FontFamily = new FontFamily("Segoe UI");
            Icon = BitmapFrame.Create(new Uri(System.IO.Path.Combine(root, "launcher.ico")));
            using (var theme = File.OpenRead(System.IO.Path.Combine(root, "Theme.xaml")))
                Resources = (ResourceDictionary)XamlReader.Load(theme);
            games = new JavaScriptSerializer().Deserialize<List<Game>>(File.ReadAllText(System.IO.Path.Combine(root, "catalog.json")));
            if (games == null || games.Count == 0) throw new InvalidDataException("Nessun collegamento configurato.");
            if (games.Select(g => g.id).Distinct().Count() != games.Count) throw new InvalidDataException("Identificativi duplicati nel catalogo.");
            library = LibraryStore.Load(settingsPath, games);
            Content = Build();
            PreviewKeyDown += delegate(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) Close(); };
            if (!previewMode && !File.Exists(settingsPath)) Loaded += delegate { SelectGames(); };
        }

        private static SolidColorBrush Brush(string value) { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(value)); }

        private static TextBlock Text(string value, double size, string color, FontWeight weight)
        {
            return new TextBlock { Text = value, FontSize = size, Foreground = Brush(color), FontWeight = weight };
        }

        private BitmapImage Picture(string name)
        {
            if (!images.ContainsKey(name))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(System.IO.Path.Combine(root, name));
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();
                images[name] = image;
            }
            return images[name];
        }

        private UIElement Build()
        {
            var shell = new Grid { Background = Brush("#101219") };
            shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
            shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(54) });
            shell.RowDefinitions.Add(new RowDefinition());
            shell.Children.Add(TopBar());
            var nav = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(34, 9, 34, 7) };
            Grid.SetRow(nav, 1);
            shell.Children.Add(nav);
            nav.Children.Add(NavButton("home", "Il tuo hub"));
            nav.Children.Add(NavButton("angrybirds", "Angry Birds"));
            nav.Children.Add(NavButton("pvz", "Plants vs. Zombies"));
            nav.Children.Add(NavButton("classics", "Classici"));
            var choose = new Button { Content = "Scegli giochi", Style = (Style)Resources["NavButton"], Background = Brush("#354252"), Foreground = Brushes.White, Padding = new Thickness(15, 7, 15, 7) };
            choose.Click += delegate { SelectGames(); };
            nav.Children.Add(choose);
            scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Padding = new Thickness(34, 20, 34, 20) };
            Grid.SetRow(scroll, 2);
            shell.Children.Add(scroll);
            page = new StackPanel();
            scroll.Content = page;
            ShowSection("home");
            return shell;
        }

        private BitmapSource Artwork(string name)
        {
            var source = Picture(name);
            if (name == "Assets/pvz-hero.png")
                return new CroppedBitmap(source, new Int32Rect(8, 8, source.PixelWidth - 16, source.PixelHeight - 16));
            return source;
        }

        private static void RoundedClip(FrameworkElement element, double radius)
        {
            element.SizeChanged += delegate { element.Clip = new RectangleGeometry(new Rect(0, 0, element.ActualWidth, element.ActualHeight), radius, radius); };
        }

        private Button NavButton(string id, string title)
        {
            var button = new Button { Content = title, Style = (Style)Resources["NavButton"], Padding = new Thickness(17, 7, 17, 7), Margin = new Thickness(0, 0, 7, 0) };
            AutomationProperties.SetName(button, "Collezione " + title);
            button.Click += delegate { ShowSection(id); };
            navigation[id] = button;
            return button;
        }

        private void ShowSection(string id)
        {
            section = id;
            foreach (var item in navigation)
            {
                item.Value.Background = Brush(item.Key == id ? "#303747" : "#171A23");
                item.Value.Foreground = Brush(item.Key == id ? "#FFFFFF" : "#929BAD");
            }
            page.Children.Clear();
            scroll.ScrollToTop();
            if (id == "home") BuildHome();
            else BuildCollection(id);
            var footer = new Grid { Margin = new Thickness(0, 18, 0, 0) };
            footer.Children.Add(Text("GAME HUB  /  LA TUA LIBRERIA", 9, "#636B7B", FontWeights.SemiBold));
            status.FontSize = 10;
            status.Foreground = Brush("#AEB5C2");
            status.HorizontalAlignment = HorizontalAlignment.Right;
            if (status.Parent != null) ((Grid)status.Parent).Children.Remove(status);
            footer.Children.Add(status);
            page.Children.Add(footer);
        }

        private void Heading(string title, string subtitle, string badge)
        {
            var intro = new Grid { Margin = new Thickness(0, 0, 0, 23) };
            intro.ColumnDefinitions.Add(new ColumnDefinition());
            intro.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var words = new StackPanel();
            words.Children.Add(Text(title, 29, "#F8F8FC", FontWeights.Bold));
            var detail = Text(subtitle, 13, "#969DAC", FontWeights.Normal);
            detail.Margin = new Thickness(0, 5, 0, 0);
            words.Children.Add(detail);
            intro.Children.Add(words);
            var count = new Border { CornerRadius = new CornerRadius(20), BorderThickness = new Thickness(1), BorderBrush = Brush("#30343F"), Padding = new Thickness(14, 8, 14, 8), VerticalAlignment = VerticalAlignment.Center };
            count.Child = Text(badge, 11, "#B6BDCB", FontWeights.SemiBold);
            Grid.SetColumn(count, 1);
            intro.Children.Add(count);
            page.Children.Add(intro);
        }

        private void BuildHome()
        {
            int collectionCount = games.Where(g => library.enabled.Contains(g.id)).Select(g => g.group).Distinct().Count();
            Heading("Il tuo prossimo gioco.", "Scegli una collezione e torna a divertirti.", collectionCount.ToString("00") + "  COLLEZIONI");
            var collections = new UniformGrid { Columns = 3, Margin = new Thickness(-7, 0, -7, 0) };
            if (games.Any(g => g.group == "angrybirds" && library.enabled.Contains(g.id))) collections.Children.Add(CollectionTile("angrybirds", "Angry Birds", "Dalla prima fionda alle corse in kart.", "Assets/hero.png", "#FFBD73"));
            if (games.Any(g => g.group == "pvz" && library.enabled.Contains(g.id))) collections.Children.Add(CollectionTile("pvz", "Plants vs. Zombies", "Il tuo giardino ha bisogno di te.", "Assets/pvz-hero.png", "#BADB87"));
            if (games.Any(g => g.group == "classics" && library.enabled.Contains(g.id))) collections.Children.Add(CollectionTile("classics", "Classici", "Piccoli giochi. Grandi ricordi.", "Assets/classics-hero.png", "#82D9E4"));
            page.Children.Add(collections);
            var note = Text(collectionCount == 0 ? "La tua libreria è vuota. Usa Scegli giochi per iniziare." : "Scegli i titoli che ami e collega le tue copie.", 12, "#798396", FontWeights.Normal);
            note.Margin = new Thickness(0, 24, 0, 10);
            note.HorizontalAlignment = HorizontalAlignment.Center;
            page.Children.Add(note);
        }

        private UIElement CollectionTile(string id, string title, string description, string image, string accent)
        {
            var button = new Button { Style = (Style)Resources["GameCard"], Height = 360, Margin = new Thickness(7, 0, 7, 0), HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
            AutomationProperties.SetName(button, "Esplora " + title);
            button.Click += delegate { ShowSection(id); };
            var grid = new Grid { ClipToBounds = true };
            RoundedClip(grid, 12);
            grid.Children.Add(new Image { Source = Artwork(image), Stretch = Stretch.UniformToFill, VerticalAlignment = VerticalAlignment.Top, Height = 360 });
            var overlay = new LinearGradientBrush { StartPoint = new Point(0.5, 0), EndPoint = new Point(0.5, 1) };
            overlay.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#1011161F"), 0));
            overlay.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#2011161F"), 0.25));
            overlay.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#F211161F"), 0.76));
            overlay.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FF11161F"), 1));
            grid.Children.Add(new Border { Background = overlay });
            var badge = new Border { Background = Brush("#CC141A25"), CornerRadius = new CornerRadius(12), Padding = new Thickness(11, 6, 11, 6), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(20) };
            var number = games.Count(g => g.group == id && library.enabled.Contains(g.id));
            badge.Child = Text(number.ToString("00") + (number == 1 ? "  GIOCO" : "  GIOCHI"), 10, "#F4F5F9", FontWeights.SemiBold);
            grid.Children.Add(badge);
            var copy = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(23, 0, 23, 25) };
            copy.Children.Add(Text(title, 25, "#FFFFFF", FontWeights.Bold));
            var detail = Text(description, 12, "#BBC4D2", FontWeights.Normal);
            detail.TextWrapping = TextWrapping.Wrap;
            detail.Margin = new Thickness(0, 5, 0, 21);
            copy.Children.Add(detail);
            var explore = Text("ESPLORA LA COLLEZIONE  \u2192", 10, accent, FontWeights.Bold);
            copy.Children.Add(explore);
            grid.Children.Add(copy);
            button.Content = grid;
            return button;
        }

        private void BuildCollection(string id)
        {
            var selection = games.Where(g => g.group == id && library.enabled.Contains(g.id)).ToList();
            if (selection.Count == 0)
            {
                Heading("Nessun gioco selezionato", "Usa Scegli giochi per aggiungere titoli a questa collezione.", "00  GIOCHI");
                return;
            }
            if (id == "pvz")
            {
                Heading("Plants vs. Zombies", "Un prato da difendere. Un’orda da fermare.", "01  GIOCO");
                page.Children.Add(Hero(selection[0]));
                return;
            }
            Heading(id == "classics" ? "Classici" : "Angry Birds", id == "classics" ? "Quelli che ti ricordi ancora." : "Fionde, galassie, kart e un sacco di guai.", selection.Count.ToString("00") + (selection.Count == 1 ? "  GIOCO" : "  GIOCHI"));
            var grid = new UniformGrid { Columns = 3, Margin = new Thickness(-6, 0, -6, 0) };
            foreach (var game in selection) grid.Children.Add(Card(game));
            page.Children.Add(grid);
        }

        public void SavePreviews(string directory)
        {
            Directory.CreateDirectory(directory);
            foreach (var section in new[] { "home", "angrybirds", "pvz", "classics" })
            {
                ShowSection(section);
                var element = (FrameworkElement)Content;
                element.Width = 1180;
                element.Height = 820;
                element.Measure(new Size(1180, 820));
                element.Arrange(new Rect(0, 0, 1180, 820));
                element.UpdateLayout();
                var bitmap = new RenderTargetBitmap(1180, 820, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(element);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (var output = File.Create(System.IO.Path.Combine(directory, section + ".png"))) encoder.Save(output);
            }
        }

        private UIElement TopBar()
        {
            var border = new Border { Background = Brush("#14161E"), BorderBrush = Brush("#262A35"), BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(22, 0, 10, 0) };
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            border.Child = row;
            row.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                if (e.ClickCount == 2) WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                else DragMove();
            };
            var brand = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            brand.Children.Add(new Image { Source = Icon, Width = 29, Height = 29, Margin = new Thickness(0, 0, 11, 0) });
            brand.Children.Add(Text("GAME HUB", 13, "#F1F2F6", FontWeights.Bold));
            var slash = Text(" / ", 13, "#4B5262", FontWeights.Normal);
            slash.Margin = new Thickness(7, 0, 7, 0);
            brand.Children.Add(slash);
            brand.Children.Add(Text("LA TUA LIBRERIA", 11, "#8E96A8", FontWeights.SemiBold));
            row.Children.Add(brand);
            var controls = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(controls, 1);
            row.Children.Add(controls);
            controls.Children.Add(WindowButton("\uE921", "Riduci a icona", delegate { WindowState = WindowState.Minimized; }));
            controls.Children.Add(WindowButton("\uE922", "Ingrandisci", delegate { WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; }));
            controls.Children.Add(WindowButton("\uE8BB", "Chiudi launcher", delegate { Close(); }));
            return border;
        }

        private Button WindowButton(string glyph, string label, Action action)
        {
            var button = new Button { Style = (Style)Resources["WindowButton"], Content = glyph, FontFamily = new FontFamily("Segoe MDL2 Assets"), ToolTip = label };
            AutomationProperties.SetName(button, label);
            button.Click += delegate { action(); };
            return button;
        }

        private UIElement Hero(Game game)
        {
            var border = new Border { Height = 350, CornerRadius = new CornerRadius(14), Background = Brush("#233249"), ClipToBounds = true };
            var grid = new Grid { ClipToBounds = true };
            RoundedClip(grid, 14);
            border.Child = grid;
            grid.Children.Add(new Image { Source = Artwork("Assets/pvz-hero.png"), Stretch = Stretch.UniformToFill, HorizontalAlignment = HorizontalAlignment.Right, Width = 830, VerticalAlignment = VerticalAlignment.Center, Height = 510, Opacity = 0.9 });
            var gradient = new LinearGradientBrush();
            gradient.StartPoint = new Point(0, 0.5);
            gradient.EndPoint = new Point(1, 0.5);
            gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FF1B2232"), 0));
            gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FA1B2232"), 0.29));
            gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#C51B2232"), 0.51));
            gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#001B2232"), 0.82));
            grid.Children.Add(new Border { Background = gradient });
            var stack = new StackPanel { Margin = new Thickness(32, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left };
            stack.Children.Add(Text("GAME OF THE YEAR EDITION  ·  " + game.year, 10, "#BADB87", FontWeights.Bold));
            var title = Text("Il prato ti aspetta.", 37, "#FFFFFF", FontWeights.Bold);
            title.Margin = new Thickness(0, 13, 0, 0);
            stack.Children.Add(title);
            var subtitle = Text("Sole, piante e qualche ospite indesiderato.", 13, "#CDD2DE", FontWeights.Normal);
            subtitle.Margin = new Thickness(0, 8, 0, 28);
            stack.Children.Add(subtitle);
            var button = new Button { Content = PlayLabel(HasCopy(game) ? "Gioca a Plants vs. Zombies" : "Collega Plants vs. Zombies", "#FFFFFF"), Style = (Style)Resources["HeroButton"], Width = 225, Height = 42, HorizontalAlignment = HorizontalAlignment.Left };
            button.Background = Brush("#568539");
            AutomationProperties.SetName(button, "Avvia " + game.name);
            button.Click += delegate { Launch(game); };
            button.ContextMenu = GameMenu(game);
            stack.Children.Add(button);
            grid.Children.Add(stack);
            return border;
        }

        private UIElement Card(Game game)
        {
            var button = new Button { Style = (Style)Resources["GameCard"], Margin = new Thickness(6, 0, 6, 12), Height = 154, HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
            AutomationProperties.SetName(button, "Avvia " + game.name);
            AutomationProperties.SetHelpText(button, "Apre il collegamento di " + game.name);
            button.Click += delegate { Launch(game); };
            button.ContextMenu = GameMenu(game);
            var layout = new Grid { Margin = new Thickness(15, 15, 15, 12) };
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(83) });
            layout.RowDefinitions.Add(new RowDefinition());
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(78) });
            layout.ColumnDefinitions.Add(new ColumnDefinition());
            var icon = new Image { Source = Picture(game.image), Width = 72, Height = 72, VerticalAlignment = VerticalAlignment.Top, Stretch = Stretch.Uniform };
            layout.Children.Add(icon);
            var info = new StackPanel { Margin = new Thickness(12, 2, 0, 0) };
            Grid.SetColumn(info, 1);
            info.Children.Add(Text(game.label + "  /  " + game.year, 8.5, game.accent, FontWeights.SemiBold));
            var name = Text(game.name.Replace("Angry Birds ", ""), 21, "#F7F8FC", FontWeights.SemiBold);
            name.Margin = new Thickness(0, 4, 0, 2);
            info.Children.Add(name);
            var description = Text(game.description, 10.5, "#9BA3B4", FontWeights.Normal);
            description.TextWrapping = TextWrapping.Wrap;
            info.Children.Add(description);
            layout.Children.Add(info);
            var baseline = new Border { BorderBrush = Brush("#292E3B"), BorderThickness = new Thickness(0, 1, 0, 0), Margin = new Thickness(0, 4, 0, 0), Padding = new Thickness(1, 9, 1, 0) };
            Grid.SetRow(baseline, 1);
            Grid.SetColumnSpan(baseline, 2);
            var line = new Grid();
            line.Children.Add(PlayLabel(HasCopy(game) ? "GIOCA" : "COLLEGA IL GIOCO", "#D7DCE8"));
            var arrow = Text("\u2197", 16, game.accent, FontWeights.Normal);
            arrow.HorizontalAlignment = HorizontalAlignment.Right;
            arrow.VerticalAlignment = VerticalAlignment.Center;
            line.Children.Add(arrow);
            baseline.Child = line;
            layout.Children.Add(baseline);
            button.Content = layout;
            return button;
        }

        private static UIElement PlayLabel(string label, string color)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var triangle = new System.Windows.Shapes.Path { Data = Geometry.Parse("M0,0 L0,9 L7,4.5 Z"), Fill = Brush(color), Width = 7, Height = 9, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 9, 0) };
            row.Children.Add(triangle);
            row.Children.Add(Text(label, 10.5, color, FontWeights.SemiBold));
            return row;
        }

        private bool HasCopy(Game game)
        {
            string path;
            return library.paths.TryGetValue(game.id, out path) && LibraryStore.IsLaunchPath(path) && File.Exists(path);
        }

        private Button ActionButton(string label, Action action)
        {
            var button = new Button { Content = label, Style = (Style)Resources["NavButton"], Background = Brush("#303c50"), Foreground = Brushes.White, Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(5) };
            button.Click += delegate { action(); };
            return button;
        }

        private void Source(Game game)
        {
            Uri uri;
            if (!Uri.TryCreate(game.sourceUrl, UriKind.Absolute, out uri) || uri.Scheme != "https") return;
            try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); }
            catch (Exception e) { MessageBox.Show(this, e.Message, "Pagina non disponibile"); }
        }

        private ContextMenu GameMenu(Game game)
        {
            var menu = new ContextMenu();
            var details = new MenuItem { Header = "Gestisci collegamento e crediti" };
            details.Click += delegate { Details(game); };
            menu.Items.Add(details);
            var source = new MenuItem { Header = "Apri pagina ufficiale" };
            source.Click += delegate { Source(game); };
            menu.Items.Add(source);
            return menu;
        }

        private bool Connect(Game game, Window owner)
        {
            var picker = new OpenFileDialog { Title = "Collega la tua copia di " + game.name, Filter = "Gioco o collegamento Windows (*.exe;*.lnk)|*.exe;*.lnk", CheckFileExists = true, Multiselect = false };
            if (picker.ShowDialog(owner) != true) return false;
            if (!LibraryStore.IsLaunchPath(picker.FileName)) return false;
            library.paths[game.id] = System.IO.Path.GetFullPath(picker.FileName);
            LibraryStore.Save(settingsPath, library);
            status.Text = game.name + " collegato";
            return true;
        }

        private Window Dialog(string title, double width, double height)
        {
            return new Window { Owner = previewMode ? null : this, Title = title, Width = width, Height = Math.Min(height, SystemParameters.WorkArea.Height * 0.90), WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Brush("#141923"), Foreground = Brushes.White, FontFamily = FontFamily, ResizeMode = ResizeMode.CanResize, Resources = Resources };
        }

        private void Details(Game game)
        {
            var dialog = Dialog(game.name, 590, 390);
            var box = new StackPanel { Margin = new Thickness(26) };
            box.Children.Add(Text(game.name, 25, "#FFFFFF", FontWeights.Bold));
            var info = Text(game.availability + "\n\nCrediti: " + game.owner + "\n\nPer Android, scegli il collegamento creato dal tuo emulatore.", 13, "#BEC7D7", FontWeights.Normal);
            info.TextWrapping = TextWrapping.Wrap;
            info.Margin = new Thickness(0, 15, 0, 16);
            box.Children.Add(info);
            box.Children.Add(ActionButton("Collega .exe o collegamento", delegate { if (Connect(game, dialog)) { dialog.Close(); ShowSection(section); } }));
            box.Children.Add(ActionButton("Apri pagina ufficiale", delegate { Source(game); }));
            dialog.Content = box;
            dialog.ShowDialog();
        }

        private Window SelectionDialog()
        {
            var dialog = Dialog("Scegli i tuoi giochi", 830, 700);
            dialog.MinWidth = 680;
            var layout = new DockPanel { Margin = new Thickness(22), Background = Brush("#141923") };
            var intro = new StackPanel { Margin = new Thickness(5, 0, 5, 15) };
            intro.Children.Add(Text("Solo i giochi che vuoi.", 26, "#FFFFFF", FontWeights.Bold));
            var help = Text("Seleziona i titoli da mostrare e collega le tue copie. Puoi cambiare tutto quando vuoi.\nIl download include il launcher. I giochi si ottengono dai rispettivi titolari.", 12, "#AAB7CB", FontWeights.Normal);
            help.TextWrapping = TextWrapping.Wrap;
            help.Margin = new Thickness(0, 7, 0, 0);
            intro.Children.Add(help);
            DockPanel.SetDock(intro, Dock.Top);
            layout.Children.Add(intro);
            var choices = new Dictionary<string, CheckBox>();
            var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
            actions.Children.Add(ActionButton("Tutti", delegate { foreach (var cb in choices.Values) cb.IsChecked = true; }));
            actions.Children.Add(ActionButton("Nessuno", delegate { foreach (var cb in choices.Values) cb.IsChecked = false; }));
            actions.Children.Add(ActionButton("Applica", delegate {
                library.enabled = choices.Where(c => c.Value.IsChecked == true).Select(c => c.Key).ToList();
                LibraryStore.Save(settingsPath, library);
                dialog.Close();
                ShowSection(section);
            }));
            DockPanel.SetDock(actions, Dock.Bottom);
            layout.Children.Add(actions);
            var list = new StackPanel();
            foreach (var game in games)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 8), Background = Brush("#1c2330") };
                row.ColumnDefinitions.Add(new ColumnDefinition());
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var text = new StackPanel();
                text.Children.Add(Text(game.name, 15, "#FFFFFF", FontWeights.SemiBold));
                text.Children.Add(Text(game.owner, 10, "#93A3B8", FontWeights.Normal));
                var cb = new CheckBox { Content = text, IsChecked = library.enabled.Contains(game.id), VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(14) };
                AutomationProperties.SetName(cb, "Includi " + game.name);
                choices[game.id] = cb;
                row.Children.Add(cb);
                var buttons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                buttons.Children.Add(ActionButton("Collega", delegate { Connect(game, dialog); }));
                buttons.Children.Add(ActionButton("Fonte", delegate { Source(game); }));
                Grid.SetColumn(buttons, 1);
                row.Children.Add(buttons);
                list.Children.Add(row);
            }
            layout.Children.Add(new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            dialog.Content = layout;
            return dialog;
        }

        private void SelectGames() { SelectionDialog().ShowDialog(); }

        public void SaveSelectionPreview(string directory)
        {
            Directory.CreateDirectory(directory);
            var dialog = SelectionDialog();
            var element = (FrameworkElement)dialog.Content;
            element.Measure(new Size(780, 650)); element.Arrange(new Rect(0, 0, 780, 650)); element.UpdateLayout();
            var bitmap = new RenderTargetBitmap(780, 650, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(element);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var output = File.Create(System.IO.Path.Combine(directory, "selection.png"))) encoder.Save(output);
        }

        private void Launch(Game game)
        {
            try
            {
                if (!HasCopy(game)) { Details(game); return; }
                var path = library.paths[game.id];
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, WorkingDirectory = System.IO.Path.GetDirectoryName(path) });
                status.Text = "Buon divertimento!";
                WindowState = WindowState.Minimized;
            }
            catch (Exception e)
            {
                MessageBox.Show(this, "Non riesco ad aprire " + game.name + ".\n\n" + e.Message, "Collegamento non disponibile", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                var application = new Application();
                bool preview = args.Length == 2 && args[0] == "--preview";
                var window = new LibraryWindow(preview ? System.IO.Path.Combine(args[1], "test-profile") : null, preview);
                if (args.Length == 2 && args[0] == "--preview")
                {
                    window.SavePreviews(args[1]);
                    window.SaveSelectionPreview(args[1]);
                    application.Shutdown();
                }
                else application.Run(window);
                return 0;
            }
            catch (Exception e)
            {
                if (args.Length > 0) File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preview-error.txt"), e.ToString());
                else MessageBox.Show("Impossibile aprire la libreria.\n\n" + e.Message, "Game Hub", MessageBoxButton.OK, MessageBoxImage.Error);
                return 1;
            }
        }
    }
}
