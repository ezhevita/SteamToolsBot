{ lib, buildDotnetModule, dotnetCorePackages }:

buildDotnetModule rec {
  pname = "steam-tools-bot";
  version = "1.0";

  src = lib.fileset.toSource {
    root = ../.;
    fileset = lib.fileset.unions [
      (lib.fileset.difference
        (lib.fileset.fileFilter (file: file.name != ".DS_Store") ../SteamToolsBot)
        (lib.fileset.unions (map lib.fileset.maybeMissing [
          ../SteamToolsBot/bin
          ../SteamToolsBot/obj
        ])))
    ];
  };

  projectFile = "SteamToolsBot/SteamToolsBot.csproj";
  nugetDeps = ./deps.json;
  dotnet-sdk = dotnetCorePackages.sdk_10_0;
  dotnet-runtime = dotnetCorePackages.runtime_10_0;
  executables = [ "SteamToolsBot" ];

  makeWrapperArgs = [
    "--set DOTNET_SYSTEM_GLOBALIZATION_INVARIANT false"
    "--add-flags --contentRoot"
    "--add-flags ${placeholder "out"}/lib/${pname}"
  ];

  meta = {
    description = "Telegram bot for querying Steam for various info.";
    license = lib.licenses.gpl3Only;
    mainProgram = "SteamToolsBot";
    platforms = lib.platforms.linux ++ lib.platforms.darwin;
  };
}
