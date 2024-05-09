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
        Rectangle[] rect = new Rectangle[2];
        Vector2[] pos = new Vector2[2];

        string serverIP = "localhost";

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
                Window.Title = "Server";
                Server();
            }
            else
            {
                Window.Title = "Client";
                Client();
            }

            
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            img = Content.Load<Texture2D>("blank");

            rect[0] = new Rectangle(100, 100, 32, 32);
            rect[1] = new Rectangle(200, 100, 32, 32);
            pos[0] = new Vector2(rect[0].X, rect[0].Y);
            pos[1] = new Vector2(rect[1].X, rect[1].Y);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (isServer)
            {
                server.PollEvents();
            }
                
            else
            {
                client.PollEvents();
            }

            
            if (Keyboard.GetState().IsKeyDown(Keys.W))
                pos[0].Y -= 3;
            if (Keyboard.GetState().IsKeyDown(Keys.S))
                pos[0].Y += 3;
            if (Keyboard.GetState().IsKeyDown(Keys.A))
                pos[0].X -= 3;
            if (Keyboard.GetState().IsKeyDown(Keys.D))
                pos[0].X += 3;
            rect[0].X = (int)pos[0].X;
            rect[0].Y = (int)pos[0].Y;

            SendVector2(pos[0]);
            
            rect[0].X = (int)pos[0].X;
            rect[0].Y = (int)pos[0].Y;
            rect[1].X = (int)pos[1].X;
            rect[1].Y = (int)pos[1].Y;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();
            // Draw your game content here if needed
            if(isServer)
            {
                spriteBatch.Draw(img, rect[0], Color.Red);
                spriteBatch.Draw(img, rect[1], Color.Green);
            }
            else
            {
                spriteBatch.Draw(img, rect[1], Color.Red);
                spriteBatch.Draw(img, rect[0], Color.Green);
                
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }

        void Server()
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
                
                writer.Put(pos[0].X + "," + pos[0].Y);                        // Put some string
                peer.Send(writer, DeliveryMethod.ReliableOrdered);  // Send with reliability
                writer.Reset();
                    
            };
            listener.NetworkReceiveEvent += (peer, dataReader, deliveryMethod, channel) =>
            {
                //Console.WriteLine("Received: {0}", dataReader.GetString(100 /* max length of string */));
                string[] data;
                string read = dataReader.GetString(100 /* max length of string */);
                Console.WriteLine("Received: {0}", read);
                data = read.Split(",");
                if (data != null)
                {
                    pos[1].X = Convert.ToInt32(data[0]);
                    pos[1].Y = Convert.ToInt32(data[1]);
                }
                dataReader.Recycle();
            };

        }

        void Client()
        {
            client = new NetManager(listener);
            client.Start();
            bool connected = false;
            while (!connected)
            {
                try
                {
                    client.Connect(serverIP /* host ip or name */, 9050 /* port */, "SomeConnectionKey" /* text key or NetDataWriter */);
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
            listener.PeerConnectedEvent += peer =>
            {
                this.peer = peer;
                Console.WriteLine("We got connection: {0}", peer);  // Show peer ip
                writer = new NetDataWriter();         // Create writer class
                //writer.Put("Hello client!");                        // Put some string
                //peer.Send(writer, DeliveryMethod.ReliableOrdered);  // Send with reliability
                
                writer.Put(pos[0].X + "," + pos[0].Y);                        // Put some string
                peer.Send(writer, DeliveryMethod.ReliableOrdered);  // Send with reliability
                writer.Reset();
                    
            };
            listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) =>
            {
                peer = fromPeer;
                //Console.WriteLine("Received: {0}", dataReader.GetString(100 /* max length of string */));
                string[] data;
                string read = dataReader.GetString(100 /* max length of string */);
                Console.WriteLine("Received: {0}", read);
                data = read.Split(",");
                if (data != null)
                {
                    
                    pos[1].X = Convert.ToInt32(data[0]);
                    pos[1].Y = Convert.ToInt32(data[1]);
                }
                dataReader.Recycle();
            };
        }

        void SendVector2(Vector2 loc)
        {
            if (isServer)
            {
                if (peer != null)
                {
                    writer.Reset();
                    writer.Put(loc.X + "," + loc.Y); // Put some string
                    peer.Send(writer, DeliveryMethod.ReliableOrdered); // Send with reliability
                    Console.WriteLine("Sent: " + loc.X + "," + loc.Y);
                    writer.Reset();
                }
            }
            else
            {
                if (peer != null)
                {
                    writer.Reset();
                    writer.Put(loc.X + "," + loc.Y); // Put some string
                    client.SendToAll(writer, DeliveryMethod.ReliableOrdered);
                    Console.WriteLine("Sent: " + loc.X + "," + loc.Y );
                }
            }
        }
    }
}
