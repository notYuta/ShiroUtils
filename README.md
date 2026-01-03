# ShiroUtils (FFXIV Dalamud Plugin)

ShiroUtils is an integrated Dalamud plugin for Final Fantasy XIV, combining several quality-of-life features into a single, convenient package. This plugin aims to enhance your gameplay experience with various overlays and tools.

## Features

Currently, ShiroUtils includes the following functionalities:

*   **Mob Hunt Overlay**: Displays the spawn locations of B-rank mob targets on your in-game map when accessed via the Hunt Bill.
*   **Gather Map Overlay**: Shows gathering nodes (mining and botany) on the main map, helping gatherers locate resources more efficiently. Icons differentiate between mining (pickaxe) and botany (leaf) nodes, with separate icons for primary and secondary tools.
*   **Quick Try On**: Allows you to quickly try on gear by holding the SHIFT key and hovering over an item in your inventory or the market board.

## Installation

1.  **Add Custom Plugin Repository:**
    *   Open your Dalamud settings (usually by typing `/xlsettings` in-game and navigating to "Experimental" or "Plugin Installer" settings).
    *   Go to the "Custom Plugin Repositories" section.
    *   Add the following URL as a new custom repository:
        `https://raw.githubusercontent.com/shironayuta/ShiroUtils/main/repo.json`
2.  **Install the Plugin:**
    *   Open the Dalamud Plugin Installer (usually `/xlplugins`).
    *   Search for "ShiroUtils".
    *   Install the plugin.

## Usage

*   **Mob Hunt Overlay**:
    *   Open your Hunt Bill.
    *   Click the "Habitat" button for a B-rank target.
    *   Mob spawn locations will be displayed on your map.
*   **Gather Map Overlay**:
    *   Switch to a Miner or Botanist job.
    *   Open your main map.
    *   Gathering nodes will appear with their respective icons.
*   **Quick Try On**:
    *   Hold down the `SHIFT` key.
    *   Hover your mouse cursor over an equippable item in your inventory, armory chest, or market board.
    *   The item will be automatically tried on. A cooldown prevents rapid successive try-ons.

## Configuration

You can access the plugin settings by typing `/xlplugins` in-game, finding "ShiroUtils", and clicking the "Settings" button. The configuration window features a PandoraBox-like layout with a left-hand navigation pane.

### General Settings

*   **Mob Hunt Overlay**: Enable/disable the mob hunt location display.
*   **Gather Map Overlay**: Enable/disable the gathering node display.
*   **Quick Try On**: Enable/disable the quick try-on functionality.

### Feature-Specific Settings

Each feature has a dedicated section for detailed configuration:

*   **Mob Hunt Overlay**:
    *   **Icon Settings (Advanced)**: Customize the map marker icon ID for B-rank mobs.
*   **Gather Map Overlay**:
    *   **Icon Settings (Advanced)**: Customize map marker icon IDs for Mining (Primary), Quarrying (Secondary), Logging (Primary), and Harvesting (Secondary) nodes.
*   **Quick Try On**:
    *   **Cooldown (ms)**: Adjust the minimum time (in milliseconds) between successive try-on actions.

## Building from Source

1.  **Prerequisites**:
    *   .NET SDK 10.0 (or compatible version with Dalamud's target framework)
    *   Visual Studio or Rider (recommended IDEs)
    *   A local Dalamud development environment set up.
2.  **Clone the Repository**:
    ```bash
    git clone https://github.com/shironayuta/ShiroUtils.git
    cd ShiroUtils
    ```
3.  **Build**:
    ```bash
    dotnet build ShiroUtils/ShiroUtils.csproj
    ```
    The compiled plugin (`ShiroUtils.dll`, `ShiroUtils.json`, etc.) will be found in `ShiroUtils/ShiroUtils/bin/Debug/`.

## Contributing

Feel free to open issues for bug reports or feature requests. Pull requests are welcome!

## License

This project is licensed under the MIT License - see the `LICENSE.md` file for details (if applicable).

---
*(Note: Replace `[TO BE PROVIDED UPON RELEASE]` with the actual repository URL for releases once available.)*
