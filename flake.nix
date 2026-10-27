{
  description = "SteamToolsBot Telegram bot";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixpkgs-unstable";

  outputs = { self, nixpkgs }:
    let
      systems = [ "x86_64-linux" "aarch64-linux" "aarch64-darwin" ];
      forAllSystems = nixpkgs.lib.genAttrs systems;
    in
    {
      packages = forAllSystems (system:
        let
          pkgs = import nixpkgs { inherit system; };
          bot = pkgs.callPackage ./nix/package.nix { };
        in
        {
          default = bot;
          steam-tools-bot = bot;
        });

      apps = forAllSystems (system: {
        default = {
          type = "app";
          program = "${self.packages.${system}.default}/bin/SteamToolsBot";
          meta.description = "Run SteamToolsBot";
        };
      });

      devShells = forAllSystems (system:
        let pkgs = import nixpkgs { inherit system; };
        in {
          default = pkgs.mkShell {
            packages = [ pkgs.dotnetCorePackages.sdk_10_0 pkgs.redis ];
            DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = "false";
          };
        });
    };
}
