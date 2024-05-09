using System;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace mgnettest
{
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        EventBasedNetListener listener = new EventBasedNetListener();
        NetManager server;
        NetManager client;
        NetDataWriter writer;

        KeyboardState kb;
        KeyboardState prevKb;

        Texture2D img;
        Rectangle rect;
        Vector2 pos;

        NetPeer peer;

        bool isServer = true;

        public Game1(bool isServer)
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            this.isServer = isServer;
        }

        protected override void Initialize()
        {
            base.Initialize();
            Console.WriteLine(isServer);

            if(isServer)
            {
                server = new NetManager(listener);
                server.Start(9050 /* port */);

                listener.ConnectionRequestEvent += request =>
                {
                    if(server.ConnectedPeersCount < 10 /* max connections */)
                        request.AcceptIfKey("SomeConnectionKey");
                    else
                        request.Reject();
                };

                listener.PeerConnectedEvent += peer =>
                {
                    this.peer = peer;
                    Console.WriteLine("We got connection: {0}", peer);  // Show peer ip
                    writer = new NetDataWriter();         // Create writer class
                    //writer.Put("Hello client!");                        // Put some string
                    //peer.Send(writer, DeliveryMethod.ReliableOrdered);  // Send with reliability
        
                    
                    Console.WriteLine("Send message: ");
                    writer.Put(pos.X + "," + pos.Y);                        // Put some string
                    peer.Send(writer, DeliveryMethod.ReliableOrdered);  // Send with reliability
                    writer.Reset();
                    
                };
            }
            else
            {
                client = new NetManager(listener);
                client.Start();
                bool connected = false;
                while (!connected)
                {
                    try
                    {
                        client.Connect("10.0.0.90" /* host ip or name */, 9050 /* port */, "SomeConnectionKey" /* text key or NetDataWriter */);
                        connected = true; // If connection succeeds, exit the loop
                        Console.WriteLine("Connected successfully!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Connection failed: {ex.Message}");
                        Console.WriteLine("Retrying...");
                        // You can add a delay here if you want to wait between connection attempts
                        // e.g., System.Threading.Thread.Sleep(1000); // Wait for 1 second
                    }
                }
                listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) =>
                {
                    //Console.WriteLine("Received: {0}", dataReader.GetString(100 /* max length of string */));
                    string[] data;
                    string read = dataReader.GetString(100 /* max length of string */);
                    Console.WriteLine("Received: {0}", read);
                    data = read.Split(",");
                    if (data != null)
                    {
                        pos.X = Convert.ToInt32(data[0]);
                        pos.Y = Convert.ToInt32(data[1]);
                    }
                    dataReader.Recycle();
                };
            }

            pos = new Vector2(rect.X, rect.Y);
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            img = Content.Load<Texture2D>("blank");

            rect = new Rectangle(100, 100, 32, 32);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (isServer)
            {
                server.PollEvents();
                if (Keyboard.GetState().IsKeyDown(Keys.W))
                    pos.Y -= 3;
                if (Keyboard.GetState().IsKeyDown(Keys.S))
                    pos.Y += 3;
                if (Keyboard.GetState().IsKeyDown(Keys.A))
                    pos.X -= 3;
                if (Keyboard.GetState().IsKeyDown(Keys.D))
                    pos.X += 3;
                rect.X = (int)pos.X;
                rect.Y = (int)pos.Y;
                
                if(peer != null)
                {
                    writer.Reset();
                    writer.Put(pos.X + "," + pos.Y); // Put some string
                    peer.Send(writer, DeliveryMethod.ReliableOrdered); // Send with reliability
                    writer.Reset();
                }
            }
                
            else
                client.PollEvents();

            rect.X = (int)pos.X;
            rect.Y = (int)pos.Y;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();
            // Draw your game content here if needed
            spriteBatch.Draw(img, rect, Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
