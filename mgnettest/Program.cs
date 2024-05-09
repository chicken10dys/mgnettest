bool isServer = args.Length > 0 && args[0] == "-s"; // Check for server flag
using var game = new mgnettest.Game1(isServer);
game.Run();