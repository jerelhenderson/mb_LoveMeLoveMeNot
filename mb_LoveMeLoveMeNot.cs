using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Globalization;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);

            EventHandler SheLovesMe_Event = new EventHandler(SheLovesMe);
            EventHandler SheLovesMeNot_Event = new EventHandler(SheLovesMeNot);
            EventHandler DontPlayWithMyLove_Event = new EventHandler(DontPlayWithMyLove);
            mbApiInterface.MB_RegisterCommand("Plugin: Love Me", SheLovesMe_Event);
            mbApiInterface.MB_RegisterCommand("Plugin: Love Me Not", SheLovesMeNot_Event);
            mbApiInterface.MB_RegisterCommand("Plugin: Love Me, Love Me Not", DontPlayWithMyLove_Event);

            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "Love Me, Love Me Not";
            about.Description = "This plugin provides a substitute for MusicBee's built-in Last.fm Love rating hotkey";
            about.Author = "The Incredible Boom Boom";
            about.TargetApplication = "";   //  the name of a Plugin Storage device or panel header for a dockable panel
            about.Type = PluginType.General;
            about.VersionMajor = 1;  // your plugin version
            about.VersionMinor = 0;
            about.Revision = 2;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
            about.ConfigurationPanelHeight = 0;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function
            return about;
        }

        public bool Configure(IntPtr panelHandle)
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            // panelHandle will only be set if you set about.ConfigurationPanelHeight to a non-zero value
            // keep in mind the panel width is scaled according to the font the user has selected
            // if about.ConfigurationPanelHeight is set to 0, you can display your own popup window
            if (panelHandle != IntPtr.Zero)
            {
                Panel configPanel = (Panel)Panel.FromHandle(panelHandle);
                Label prompt = new Label();
                prompt.AutoSize = true;
                prompt.Location = new Point(0, 0);
                prompt.Text = "prompt:";
                TextBox textBox = new TextBox();
                textBox.Bounds = new Rectangle(60, 0, 100, textBox.Height);
                configPanel.Controls.AddRange(new Control[] { prompt, textBox });
            }
            return false;
        }




        /* PROJECT START */

        public void SheLovesMe(object sender, EventArgs e)
        {
            string[] selectedTracks = GatherSelectedTracks();

            LoveQuestion(null, selectedTracks, "SheNeedsMyLove");
        }

        public void SheLovesMeNot(object sender, EventArgs e)
        {
            string[] selectedTracks = GatherSelectedTracks();

            LoveQuestion(null, selectedTracks, "DontWantYourLove");
        }

        public void DontPlayWithMyLove(object sender, EventArgs e)
        {
            string file = mbApiInterface.NowPlaying_GetFileProperty(FilePropertyType.Url);
            LoveQuestion(file, null, mbApiInterface.NowPlaying_GetFileTag(MetaDataType.RatingLove));
        }

        private string[] GatherSelectedTracks()
        {
            string[] selected = new string[] { };
            mbApiInterface.Library_QueryFilesEx("domain=SelectedFiles", out selected);

            return selected;
        }

        private void LoveQuestion(string file, string[] files, string loveAnswer)
        {
            if (file != null)
            {
                if (loveAnswer != "L") CommitToTags(file, "L");
            } else if (files.Length == 0 && loveAnswer == "DontWantYourLove") {
                CommitToTags(mbApiInterface.NowPlaying_GetFileProperty(FilePropertyType.Url), "");
            } else {
                foreach (string track in files)
                {
                    if (mbApiInterface.Library_GetFileTag(track, MetaDataType.RatingLove) != "L" && loveAnswer == "SheNeedsMyLove") {
                        CommitToTags(track, "L");
                    } else if (mbApiInterface.Library_GetFileTag(track, MetaDataType.RatingLove) == "L" && loveAnswer == "DontWantYourLove") {
                        CommitToTags(track, "");
                    } else {
                        continue;
                    }
                }
            }
            mbApiInterface.MB_RefreshPanels();
        }

        private void CommitToTags(string file, string loveOrNah)
        {
            mbApiInterface.Library_SetFileTag(file, MetaDataType.RatingLove, loveOrNah);
            mbApiInterface.Library_CommitTagsToFile(file);
        }

        /* PROJECT END*/





        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        {
        }

        // uninstall this plugin - clean up any persisted files
        public void Uninstall()
        {
        }

        // receive event notifications from MusicBee
        // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            // perform some action depending on the notification type
            switch (type)
            {
                case NotificationType.PluginStartup:
                    // perform startup initialisation
                    switch (mbApiInterface.Player_GetPlayState())
                    {
                        case PlayState.Playing:
                        case PlayState.Paused:
                            // ...
                            break;
                    }
                    break;
                case NotificationType.TrackChanged:
                    string artist = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artist);
                    // ...
                    break;
            }
        }

        // return an array of lyric or artwork provider names this plugin supports
        // the providers will be iterated through one by one and passed to the RetrieveLyrics/ RetrieveArtwork function in order set by the user in the MusicBee Tags(2) preferences screen until a match is found
        //public string[] GetProviders()
        //{
        //    return null;
        //}

        // return lyrics for the requested artist/title from the requested provider
        // only required if PluginType = LyricsRetrieval
        // return null if no lyrics are found
        //public string RetrieveLyrics(string sourceFileUrl, string artist, string trackTitle, string album, bool synchronisedPreferred, string provider)
        //{
        //    return null;
        //}

        // return Base64 string representation of the artwork binary data from the requested provider
        // only required if PluginType = ArtworkRetrieval
        // return null if no artwork is found
        //public string RetrieveArtwork(string sourceFileUrl, string albumArtist, string album, string provider)
        //{
        //    //Return Convert.ToBase64String(artworkBinaryData)
        //    return null;
        //}

        //  presence of this function indicates to MusicBee that this plugin has a dockable panel. MusicBee will create the control and pass it as the panel parameter
        //  you can add your own controls to the panel if needed
        //  you can control the scrollable area of the panel using the mbApiInterface.MB_SetPanelScrollableArea function
        //  to set a MusicBee header for the panel, set about.TargetApplication in the Initialise function above to the panel header text
        //public int OnDockablePanelCreated(Control panel)
        //{
        //  //    return the height of the panel and perform any initialisation here
        //  //    MusicBee will call panel.Dispose() when the user removes this panel from the layout configuration
        //  //    < 0 indicates to MusicBee this control is resizable and should be sized to fill the panel it is docked to in MusicBee
        //  //    = 0 indicates to MusicBee this control resizeable
        //  //    > 0 indicates to MusicBee the fixed height for the control.Note it is recommended you scale the height for high DPI screens(create a graphics object and get the DpiY value)
        //    float dpiScaling = 0;
        //    using (Graphics g = panel.CreateGraphics())
        //    {
        //        dpiScaling = g.DpiY / 96f;
        //    }
        //    panel.Paint += panel_Paint;
        //    return Convert.ToInt32(100 * dpiScaling);
        //}

        // presence of this function indicates to MusicBee that the dockable panel created above will show menu items when the panel header is clicked
        // return the list of ToolStripMenuItems that will be displayed
        //public List<ToolStripItem> GetHeaderMenuItems()
        //{
        //    List<ToolStripItem> list = new List<ToolStripItem>();
        //    list.Add(new ToolStripMenuItem("A menu item"));
        //    return list;
        //}

        //private void panel_Paint(object sender, PaintEventArgs e)
        //{
        //    e.Graphics.Clear(Color.Red);
        //    TextRenderer.DrawText(e.Graphics, "hello", SystemFonts.CaptionFont, new Point(10, 10), Color.Blue);
        //}

    }
}