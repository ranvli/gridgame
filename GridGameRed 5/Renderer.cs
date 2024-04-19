using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GridGameRed
{
    public class Renderer
    {
        private Thread renderThread;
        private GameEngine gameEngine;
        private bool running = true;
        private Form renderForm;

        public Renderer(GameEngine engine, Form form)
        {
            gameEngine = engine;
            renderForm = form;
            renderThread = new Thread(RenderLoop);
            renderThread.IsBackground = true;
        }

        private void RenderLoop()
        {
            while (running)
            {
                // Request the form to redraw
                renderForm.Invoke(new Action(renderForm.Refresh));
                Thread.Sleep(1000 / 24); // Sleep to aim for at least 24fps
            }
        }

        public void Start()
        {
            renderThread.Start();
        }

        public void Stop()
        {
            running = false;
            renderThread.Join(); // Safely stop the thread
        }
    }
}
