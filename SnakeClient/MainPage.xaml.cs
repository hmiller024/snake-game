using Controller;

namespace SnakeGame;

public partial class MainPage : ContentPage
{
    GameController gc = new GameController();
    public MainPage()
    {
        InitializeComponent();
        graphicsView.Invalidate();
        worldPanel.SetWorld(gc.GetWorld());
        gc.GameUpdate += OnFrame;
        gc.ErrorEvent += NetworkErrorHandler;
        
    }

    void OnTapped(object sender, EventArgs args)
    {
        keyboardHack.Focus();
    }

    /// <summary>
    /// This method handles keyboard input for moving snake
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The <see cref="TextChangedEventArgs"/> instance containing the event data.</param>
    void OnTextChanged(object sender, TextChangedEventArgs args)
    {
        Entry entry = (Entry)sender;
        String text = entry.Text.ToLower();
        if (text == "w")
        {
            // Move up
            gc.InputW();
        }
        else if (text == "a")
        {
            // Move left
            gc.InputA();
        }
        else if (text == "s")
        {
            // Move down
            gc.InputS();
        }
        else if (text == "d")
        {
            // Move right
            gc.InputD();
        }
        entry.Text = "";
    }

    /// <summary>
    /// Event handler for failed network connection
    /// </summary>
    private void NetworkErrorHandler()
    {        
        Dispatcher.Dispatch(() => DisplayAlert("Error", "Connection Failed! Please try again!", "OK"));
        Dispatcher.Dispatch(() => connectButton.IsEnabled = true);
        Dispatcher.Dispatch(() => connectButton.BackgroundColor = Colors.DarkGoldenrod);
    }


    /// <summary>
    /// Event handler for the connect button
    /// We will put the connection attempt logic here in the view, instead of the controller,
    /// because it is closely tied with disabling/enabling buttons, and showing dialogs.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void ConnectClick(object sender, EventArgs args)
    {
        if (serverText.Text == "")
        {
            DisplayAlert("Error", "Please enter a server address", "OK");
            return;
        }
        if (nameText.Text == "")
        {
            DisplayAlert("Error", "Please enter a name", "OK");
            return;
        }
        if (nameText.Text.Length > 16)
        {
            DisplayAlert("Error", "Name must be less than 16 characters", "OK");
            return;
        }
     
        keyboardHack.Focus();

        gc.Connect(serverText.Text, nameText.Text);
        connectButton.IsEnabled = false;
        connectButton.BackgroundColor = Colors.Grey;
    }

    /// <summary>
    /// Use this method as an event handler for when the controller has updated the world
    /// </summary>
    public void OnFrame()
    {
        worldPanel.SetWorld(gc.GetWorld());
        Dispatcher.Dispatch( () => graphicsView.Invalidate() );
    }

    /// <summary>
    /// Handles the Clicked event of the ControlsButton control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void ControlsButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("Controls",
                     "W:\t\t Move up\n" +
                     "A:\t\t Move left\n" +
                     "S:\t\t Move down\n" +
                     "D:\t\t Move right\n",
                     "OK");
    }

    /// <summary>
    /// Handles the Clicked event of the AboutButton control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void AboutButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("About",
      "SnakeGame solution\nArtwork by Jolie Uk and Alex Smith\nGame design by Daniel Kopta and Travis Martin\n" +
      "Implementation by Frank Chen and Hayden Miller\n" +
        "CS 3500 Fall 2022, University of Utah", "OK");
    }

    /// <summary>
    /// Handles the Focused event of the ContentPage control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="FocusEventArgs"/> instance containing the event data.</param>
    private void ContentPage_Focused(object sender, FocusEventArgs e)
    {
        if (!connectButton.IsEnabled)
            keyboardHack.Focus();
    }
}