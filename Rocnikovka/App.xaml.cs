namespace Rocnikovka;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // TENTO ŘÁDEK ZABALÍ MAINPAGE DO NAVIGACE, ABY ŠLA OTEVŘÍT HISTORIE
        MainPage = new NavigationPage(new MainPage());
    }
}