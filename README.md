# SteamToolsBot

Telegram bot for querying Steam for various info. Currently only available command is `/5dollar`, which shows the Steam conversion of 5 USD to RUB.

## Nix

Development shell:

```sh
nix develop path:.
cd SteamToolsBot
dotnet run
```

Regenerate the dependency hashes:

```sh
nix build path:.#default.fetch-deps --out-link /tmp/steam-tools-bot-fetch-deps
/tmp/steam-tools-bot-fetch-deps "$PWD/nix/deps.json"
nix build path:.
```

Update nixpkgs:

```sh
nix flake update --flake path:.
```
