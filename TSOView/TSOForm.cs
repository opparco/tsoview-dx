using System;
using System.Collections.Generic;
using System.Diagnostics;
//using System.Drawing;
//using System.Threading;
//using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
//using CSScriptLibrary;
using TDCG;

using SharpDX.Windows;
using SharpDX.DirectInput;

namespace TSOView
{
[System.Runtime.InteropServices.ComVisible(false)]
public partial class TSOForm : RenderForm
{
    internal bool userResized = true;

    DirectInput directInput = null;
    Keyboard keyboard = null;
    KeyboardState keyboardState = null;

    internal Key keySave = Key.Return;
    internal Key keyMotion = Key.Space;
    internal Key keyShadow = Key.S;
    internal Key keySprite = Key.Z;
    internal Key keyFigure = Key.Tab;
    internal Key keyDelete = Key.Delete;
    internal Key keyCameraReset = Key.D0;
    internal Key keyCenter = Key.F;
    internal Key keyFigureForm = Key.G;

    internal Viewer viewer = null;
    internal FigureForm fig_form = null;
    
    string save_path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\TechArts3D\TDCG";

    TSOConfig tso_config;

    public TSOForm(TSOConfig tso_config, string[] args)
    {
        this.tso_config = tso_config;

        InitializeComponent();
        this.ClientSize = tso_config.ClientSize;

        this.DragDrop += new DragEventHandler(form_OnDragDrop);
        this.DragOver += new DragEventHandler(form_OnDragOver);

        this.UserResized += delegate(Object sender, EventArgs e)
        {
            userResized = true;
        };

        this.viewer = new Viewer();
        viewer.ScreenColor = tso_config.ScreenColor;

        this.fig_form = new FigureForm();

        if (viewer.InitializeApplication(this, tso_config))
        {
            viewer.figures.FigureEvent += delegate(object sender, EventArgs e)
            {
                Figure fig;
                if (viewer.figures.TryGetFigure(out fig))
                    fig_form.SetFigure(fig);
                else
                    fig_form.Clear();
            };
            viewer.Camera.SetTranslation(tso_config.Position);
            foreach (string arg in args)
                viewer.figures.LoadAnyFile(arg, true);
            if (viewer.figures.Count == 0)
                viewer.figures.LoadAnyFile(Path.Combine(save_path, @"system.tdcgsav.png"), true);
            //this.timer1.Enabled = true;
        }

        directInput = new DirectInput();
        keyboard = new Keyboard(directInput);
        keyboard.Acquire();

        keyboardState = keyboard.GetCurrentState();
    }

    static float DegreeToRadian(float angle)
    {
        return (float)(Math.PI * angle / 180.0);
    }

    public void FrameMove()
    {
        if (userResized)
        {
            userResized = false;
            viewer.OnUserResized();
        }

        KeyboardState currentKeyboardState = keyboard.GetCurrentState();

        if (!keyboardState.IsPressed(keySave) && currentKeyboardState.IsPressed(keySave))
        {
            viewer.SaveToBitmap("sample.png");
        }

        if (!keyboardState.IsPressed(keyMotion) && currentKeyboardState.IsPressed(keyMotion))
        {
            viewer.MotionEnabled = !viewer.MotionEnabled;
        }
        if (!keyboardState.IsPressed(keyFigure) && currentKeyboardState.IsPressed(keyFigure))
        {
            viewer.figures.NextFigure();
        }
        if (!keyboardState.IsPressed(keyDelete) && currentKeyboardState.IsPressed(keyDelete))
        {
            if (currentKeyboardState.IsPressed(Key.LeftControl) || currentKeyboardState.IsPressed(Key.RightControl))
                viewer.figures.ClearFigureList();
            else
                viewer.figures.RemoveSelectedFigure();
        }
        if (!keyboardState.IsPressed(keyCameraReset) && currentKeyboardState.IsPressed(keyCameraReset))
        {
            viewer.Camera.Reset();
            viewer.Camera.SetTranslation(tso_config.Position);
        }
        if (!keyboardState.IsPressed(keyCenter) && currentKeyboardState.IsPressed(keyCenter))
        {
            viewer.Camera.ResetTranslation();
            Figure fig;
            if (viewer.figures.TryGetFigure(out fig))
                viewer.Camera.SetCenter(fig.Center + fig.Translation);
        }
        if (!keyboardState.IsPressed(keyFigureForm) && currentKeyboardState.IsPressed(keyFigureForm))
        {
            fig_form.Show();
            fig_form.Activate();
        }

        float keyL = 0.0f;
        float keyR = 0.0f;
        float keyU = 0.0f;
        float keyD = 0.0f;
        float keyPush = 0.0f;
        float keyPull = 0.0f;
        float keyZRol = 0.0f;

        if (currentKeyboardState.IsPressed(Key.Left))
            keyL = 0.01f;
        if (currentKeyboardState.IsPressed(Key.Right))
            keyR = 0.01f;
        if (currentKeyboardState.IsPressed(Key.PageUp))
            keyU = 0.01f;
        if (currentKeyboardState.IsPressed(Key.PageDown))
            keyD = 0.01f;
        if (currentKeyboardState.IsPressed(Key.Up))
            keyPush = 0.5f;
        if (currentKeyboardState.IsPressed(Key.Down))
            keyPull = 0.5f;
        if (currentKeyboardState.IsPressed(Key.A))
            keyZRol = -1.0f;
        if (currentKeyboardState.IsPressed(Key.D))
            keyZRol = +1.0f;

        if (Control.ModifierKeys == Keys.Shift)
        {
            Figure fig;
            if (viewer.figures.TryGetFigure(out fig))
                fig.Move(keyR - keyL, keyU - keyD, keyPull - keyPush);
        }
        else
        {
            viewer.Camera.RotateYawPitchRoll(keyL - keyR, keyU - keyD, DegreeToRadian(keyZRol));
            viewer.Camera.MoveView(0, 0, keyPull - keyPush);
        }
        viewer.FrameMove();
        viewer.Render();

        keyboardState = currentKeyboardState;
    }

    private void form_OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            if ((e.KeyState & 8) == 8)
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.Move;
        }
    }

    private void form_OnDragDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            foreach (string src in (string[])e.Data.GetData(DataFormats.FileDrop))
                viewer.figures.LoadAnyFile(src, (e.KeyState & 8) == 8);
        }
    }

    private void timer1_Tick(object sender, EventArgs e)
    {
        this.FrameMove();
        viewer.FrameMove();
        viewer.Render();
    }

    protected override void OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
    {
        if ((int)(byte)e.KeyChar == (int)System.Windows.Forms.Keys.Escape)
            this.Dispose(); // Esc was pressed
    }
}
}
