using System.Collections.Generic;
using Beebyte.Obfuscator;
using SFS.Translations;
using UnityEngine.Scripting;

namespace SFS;

[Preserve]
[Skip]
public class SFS_Translation
{
	private readonly Dictionary<string, Field> fields = new Dictionary<string, Field>();

	public Field None => A("None", "None");

	public Field Font => A("Font", "normal");

	[Group("General")]
	public Field Cancel => A("Cancel", "Cancel");

	public Field Close => A("Close", "Close");

	[LocSpace(1, false)]
	public Field On => A("On", "On");

	public Field Off => A("Off", "Off");

	[LocSpace(1, false)]
	public Field Open_Settings_Button => A("Open_Settings_Button", "Settings");

	public Field Open_Cheats_Button => A("Open_Cheats_Button", "Cheats");

	public Field Help => A("Help", "Help");

	[LocSpace(1, false)]
	public Field Build_Rocket => A("Build_Rocket", "Build Rocket");

	public Field Resume_Game => A("Resume_Game", "Resume Game");

	public Field Return_To_Main_Menu => A("Return_To_Main_Menu", "Main Menu");

	public Field Exit_To_Main_Menu => A("Exit_To_Main_Menu", "Exit To Main Menu");

	[Group("Main Menu")]
	public Field Play => A("Play", "Play");

	[LocSpace(1, false)]
	public Field Video_Tutorials_OpenButton => A("Video_Tutorials_OpenButton", "Video Tutorials");

	public Field Video_Orbit => A("Video_Orbit", "Orbit Tutorial");

	public Field Video_Moon => A("Video_Moon", "Moon Tutorial");

	public Field Video_Dock => A("Video_Dock", "Docking Tutorial");

	[LocSpace(1, false)]
	public Field Development_Preview => A("Development_Preview", "Development Preview");

	public Field Development_Preview_Explanation => A("Development_Preview_Explanation", "The development preview showcases future updates we are working on");

	[LocSpace(1, false)]
	public Field Community_OpenButton => A("Community_OpenButton", "Community");

	public Field Community_Youtube => A("Community_Youtube", "Youtube");

	public Field Community_Discord => A("Community_Discord", "Discord");

	public Field Community_Reddit => A("Community_Reddit", "Reddit");

	public Field Community_Forums => A("Community_Forums", "Forums");

	[LocSpace(1, false)]
	public Field Credits_OpenButton => A("Credits_OpenButton", "Credits");

	public Field Credits_Text => A("Credits_Text", Field.MultilineText("<Size=70> Štefo Mai Morojna </size>", "<Size=55> Designer - Programmer - Artist </size>", "", "<Size=70> Jordi van der Molen </size>", "<Size=55> Programmer </size>", "", "<Size=70> Chris Christo </size>", "<Size=55> Programmer </size>", "", "<Size=70> Josh </size>", "<Size=55> Programmer </size>", "", "<Size=70> Aidan Ginise </size>", "<Size=55> Programmer </size>", "", "<Size=70> Andrey Onischenko </size>", "<Size=55> Programmer </size>", "", "<Size=70> Davi Vasc </size>", "<Size=55> Composer </size>", "", "<Size=70> Ashton Mills </size>", "<size=55> Composer </size>"));

	[LocSpace(1, false)]
	public Field First_Time_Playing_Question => A("First_Time_Playing_Question", "First time playing\nSpaceflight Simulator?");

	public Field First_Time_Playing_Yes => A("First_Time_Playing_Yes", "First Time Playing");

	public Field First_Time_Playing_No => A("First_Time_Playing_No", "Played Before");

	[LocSpace(1, false)]
	public Field Update_Available => A("Update_Available", "A new update is available!\n\nCurrent version: %old%\nLatest version: %new%");

	public Field Update_Confirm => A("Update_Confirm", "Update");

	[LocSpace(1, false)]
	public Field Rate_Title => A("Rate_Title", Field.MultilineText("Would you like to rate or review the game?", "", "We deeply care about the quality of our game, your feedback helps us improve it", "", "Even after thousands of reviews, we still read a large number of them!"));

	public Field Rate_Confirm => A("Rate_Confirm", "Rate");

	[LocSpace(1, false)]
	public Field Exit_Button => A("Exit_Button", "Exit");

	public Field Close_Game => A("Close_Game", "Close game?");

	[Group("Worlds Menu")]
	public Field Create_New_World_Button => A("Create_New_World_Button", "Create New World");

	public Field World_Delete => A("World_Delete", "Delete world?");

	[Documentation("Create menu", false, false)]
	public Field Create_World_Title => A("Create_World_Title", "World Name");

	public Field Default_World_Name => A("Default_World_Name", "My World");

	public Field Select_Solar_System => A("Select_Solar_System", "Select world's solar system");

	public Field Select_Solar_System__NotFound => A("Select_Solar_System__NotFound", Field.MultilineText("Solar system could not be found:", "%system%", "", "Select a new solar system"));

	public Field Default_Solar_System => A("Default_Solar_System", "Solar System (Default)");

	public Field Custom_Solar_System => A("Custom_Solar_System", "%name% (Custom)");

	[Documentation("World info", false, false)]
	public Field World_Mode_Name => A("World_Mode_Name", "Mode: %value%");

	public Field Mode_Sandbox => A("Mode_Sandbox", "Sandbox");

	public Field Mode_Challenge => A("Mode_Challenge", "Challenge");

	public Field Mode_Career => A("Mode_Career", "Career");

	[LocSpace(1, false)]
	public Field Allow_Cheats_Name => A("Allow_Cheats_Name", "Allow Cheats: %value%");

	public Field Allow_Cheats_Label => A("Allow_Cheats_Label", "Allow Cheats");

	[LocSpace(1, false)]
	public Field Allow_Quicksaves_Name => A("Allow_Quicksaves_Name", "Allow Quicksaves: %value%");

	public Field Allow_Quicksaves_Label => A("Allow_Quicksaves_Label", "Allow Quicksaves");

	[LocSpace(1, false)]
	public Field World_Difficulty_Name => A("World_Difficulty_Name", "Difficulty: %value%");

	public Field Difficulty_Normal => A("Difficulty_Normal", "Normal");

	public Field Difficulty_Hard => A("Difficulty_Hard", "Hard");

	public Field Difficulty_Realistic => A("Difficulty_Realistic", "Realistic");

	[LocSpace(1, false)]
	public Field Last_Played => A("Last_Played", "Last played: %value% ago");

	public Field Just_Played => A("Just_Played", "Last played: A moment ago");

	public Field Time_Played => A("Time_Played", "Playtime: %value%");

	[Group("World Create Menu")]
	public Field World_Create_Title => A("World_Create_Title", "Create World");

	public Field World_Name_Label => A("World_Name_Label", "World Name:");

	public Field Solar_System_Label => A("Solar_System_Label", "Solar System:");

	public Field Mode_Label => A("Mode_Label", "Mode:");

	public Field Difficulty_Label => A("Difficulty_Label", "Difficulty:");

	[LocSpace(1, false)]
	[Unexported]
	public Field Difficulty_Scale_Stat => A("Difficulty_Scale_Stat", "Scale: 1:%scale%");

	[Unexported]
	public Field Difficulty_Isp_Stat => A("Difficulty_Isp_Stat", "Specific Impulse: %value%x");

	[Unexported]
	public Field Difficulty_Dry_Mass_Stat => A("Difficulty_Dry_Mass_Stat", "Tank Dry Mass: %value%x");

	[Unexported]
	public Field Difficulty_Engine_Mass_Stat => A("Difficulty_Engine_Mass_Stat", "Engine Mass: %value%x");

	[Group("Saving")]
	[Documentation("Blueprint stuff", false, false)]
	public Field Blueprints_Menu_Title => A("Blueprints_Menu_Title", "Blueprints");

	public Field Unnamed_Blueprint => A("Unnamed_Blueprint", "Unnamed Blueprint");

	public Field Save_Blueprint => A("Save_Blueprint", "Save Blueprint");

	public Field Load_Blueprint => A("Load_Blueprint", "Load Blueprint");

	public Field Cannot_Save_Empty_Build => A("Cannot_Save_Empty_Build", "Cannot save an empty blueprint");

	[Documentation("Quicksave stuff", false, false)]
	public Field Quicksaves_Menu_Title => A("Quicksaves_Menu_Title", "Quicksaves");

	public Field Unnamed_Quicksave => A("Unnamed_Quicksave", "Unnamed Quicksave");

	public Field Create_Quicksave => A("Create_Quicksave", "Create Quicksave");

	public Field Load_Quicksave => A("Load_Quicksave", "Load Quicksave");

	[Documentation("Save and load menus", false, false)]
	public Field Save => A("Save", "Save");

	public Field Load => A("Load", "Load");

	public Field Import => A("Import", "Import");

	public Field Delete => A("Delete", "Delete");

	public Field Rename => A("Rename", "Rename");

	public Field Delete_File_With_Type => A("Delete_File_With_Type", "Delete %filename% %filetype{1}%");

	[Documentation("In progress", false, false)]
	public Field Saving_In_Progress => A("Saving_In_Progress", "Saving...");

	public Field Loading_In_Progress => A("Loading_In_Progress", "Loading...");

	public Field Importing_In_Progress => A("Importing_In_Progress", "Importing...");

	[Documentation("filetype (injected)", false, false)]
	public Field Blueprint => A("Blueprint", Field.Subs("Blueprint", "blueprint", "blueprints"));

	public Field Quicksave => A("Quicksave", Field.Subs("Quicksave", "quicksave", "quicksaves"));

	[Documentation("Ask overwrite menu", false, false)]
	public Field File_Already_Exists => A("File_Already_Exists", "A %filetype{1}% with this name already exists");

	public Field Overwrite_File => A("Overwrite_File", "Overwrite %filetype{1}%");

	public Field New_File => A("New_File", "New %filetype{1}%");

	[Documentation("Load failure", false, false)]
	public Field Load_Failed => A("Load_Failed", "Could not load %filetype{1}% from %filepath%");

	[Group("Purchasing")]
	public Field Open_Shop_Menu => A("Open_Shop_Menu", "Expansions");

	public Field Parts_Expansion => A("Parts_Expansion", "Parts Expansion");

	public Field Redstone_Atlas_Pack => A("Redstone_Atlas_Pack", "Redstone Atlas Pack");

	public Field Skins_Expansion => A("Skins_Expansion", "Skins Expansion");

	public Field Planets_Expansion => A("Planets_Expansion", "Planets Expansion");

	public Field Cheats_Expansion => A("Cheats_Expansion", "Cheats");

	public Field Infinite_Area_Expansion => A("Infinite_Area_Expansion", "Infinite Build Area");

	public Field Builder_Bundle => A("Builder_Bundle", "Builder Bundle");

	public Field Sandbox_Bundle => A("Sandbox_Bundle", "Sandbox Bundle");

	public Field Full_Bundle => A("Full_Bundle", "Full Bundle");

	public Field Upgrade_To_Full_Bundle => A("Upgrade_To_Full_Bundle", "Upgrade To Full Bundle");

	public Field Mac_Full_Version => A("Mac_Full_Version", "Full Version");

	public Field Not_All_Parts_Are_Owned => A("Not_All_Parts_Are_Owned", "Not all parts are owned\nDisabled not owned parts\n\nView parts expansion?");

	public Field View_Part_Expansion => A("View_Part_Expansion", "View Expansion");

	[LocSpace(1, false)]
	public Field More_Parts => A("More_Parts", "View Parts Expansion");

	public Field More_Skins => A("More_Skins", "View Skins Expansion");

	public Field Cannot_Use_Cheats_In_Career => A("Cannot_Use_Cheats_In_Career", "Cheats can only be used in a sandbox mode world");

	public Field Get_Infinite_Build_Area_Button => A("Get_Infinite_Build_Area_Button", "Get Infinite Build Area");

	public Field Get_Cheats_Expansion_Button => A("Get_Cheats_Expansion_Button", "Get Cheats Expansion");

	[LocSpace(1, false)]
	public Field Buy_Product => A("Buy_Product", "Buy %product% %price%");

	public Field Timed_Sale_Text => A("Timed_Sale_Text", "%product_name% -%sale_percent%\n%time_left%");

	public Field Time_Upgrade_Text => A("Time_Upgrade_Text", "Upgrade to %product_name% -%sale_percent%\n%time_left%");

	public Field Purchase_Thanks_Msg => A("Purchase_Thanks_Msg", Field.MultilineText("Purchased: %product%", "", "Thanks for your support!", "Now go and explore the stars!"));

	public Field Owned => A("Owned", "%product% (Owned)");

	[LocSpace(1, false)]
	public Field Restore_Open => A("Restore_Open", "Restore Purchases");

	[Documentation("Parts Expansion", false, false)]
	public Field PartsExpansion_Tanks => A("PartsExpansion_Tanks", "Large variety of fuel tanks!");

	public Field PartsExpansion_Engines => A("PartsExpansion_Engines", "Heavy lift engines!");

	public Field PartsExpansion_Parts => A("PartsExpansion_Parts", "Parts of all shapes and sizes!");

	public Field PartsExpansion_Build => A("PartsExpansion_Build", "Large build space to bring\nyour creations to life!");

	[Documentation("Skins Expansion", false, false)]
	public Field SkinsExpansion_Tanks => A("SkinsExpansion_Tanks", "Paint your parts in a diverse variety of skins!");

	public Field SkinsExpansion_Interstages => A("SkinsExpansion_Interstages", "Color everything from interstages");

	public Field SkinsExpansion_Nosecones => A("SkinsExpansion_Nosecones", "To nosecones");

	public Field SkinsExpansion_Fairings => A("SkinsExpansion_Fairings", "And even fairings");

	[Documentation("Planets Expansion", false, false)]
	public Field PlanetsExpansion_Jupiter => A("PlanetsExpansion_Jupiter", "Explore Jupiter and its four moons!");

	public Field PlanetsExpansion_Callisto => A("PlanetsExpansion_Callisto", "From the heavily cratered surface of Callisto!");

	public Field PlanetsExpansion_Europa => A("PlanetsExpansion_Europa", "To the vast ice flats of Europa!");

	public Field PlanetsExpansion_Conclusion => A("PlanetsExpansion_Conclusion", "Distant worlds are waiting for you\nto explore them!");

	[Group("Sharing")]
	public Field Share_Button => A("Share_Button", "Share Blueprint");

	public Field Upload_Blueprint_PC => A("Upload_Blueprint_PC", "Upload Blueprint");

	public Field Download_Blueprint_PC => A("Download_Blueprint_PC", "Download Blueprint");

	public Field Share_Button_PC => A("Share_Button_PC", "Share");

	public Field Download_Confirm => A("Download_Confirm", "Download");

	public Field URL_Field_TextBox => A("URL_Field_TextBox", "Blueprint URL");

	public Field Empty_Upload => A("Empty_Upload", "Cannot upload empty blueprint");

	public Field Uploading_Message => A("Uploading_Message", "Uploading...");

	public Field Upload_Fail => A("Upload_Fail", "Failed to upload blueprint");

	public Field Copied_URL_To_Clipboard => A("Copied_URL_To_Clipboard", "Copied blueprint URL to clipboard");

	public Field Sharing_Enter_Prompt => A("Sharing_Enter_Prompt", "Select which world you want blueprint to be loaded into");

	public Field Confirm_Download_Button => A("Confirm_Download_Button", "Download Blueprint");

	public Field Downloading_Message => A("Downloading_Message", "Downloading...");

	public Field Download_Fail => A("Download_Fail", "Failed to download blueprint");

	public Field URL_Invalid => A("URL_Invalid", "Invalid Blueprint URL");

	public Field Sharing_Connect_Fail => A("Sharing_Connect_Fail", "Could not connect to sharing servers");

	[Group("Setting Titles PC")]
	[Unexported]
	public Field General_Title => A("General_Title", "General Settings");

	[Unexported]
	public Field Video_Title => A("Video_Title", "Video Settings");

	[Unexported]
	public Field Audio_Title => A("Audio_Title", "Audio Settings");

	[Unexported]
	public Field Keybindings_Title => A("Keybindings_Title", "Keybindings");

	[Group("Settings PC")]
	[Unexported]
	public Field Video_Resolution_Name => A("Video_Resolution_Name", "Resolution");

	[Unexported]
	public Field Video_WindowMode_Name => A("Video_WindowMode_Name", "Window mode");

	[Unexported]
	public Field Fullscreen_Exclusive => A("Fullscreen_Exclusive", "Fullscreen");

	[Unexported]
	public Field Fullscreen_Borderless => A("Fullscreen_Borderless", "Borderless");

	[Unexported]
	public Field Fullscreen_Windowed => A("Fullscreen_Windowed", "Windowed");

	[Unexported]
	public Field Fps_Unlimited => A("Fps_Unlimited", "Unlimited");

	[Unexported]
	public Field Video_VerticalSync_Name => A("Video_VerticalSync_Name", "Vertical Sync");

	[Group("Settings Mobile")]
	public Field Music_Name => A("Music_Name", "Music");

	public Field Sound_Name => A("Sound_Name", "Sound");

	public Field Screen_Rotation_Name => A("Screen_Rotation_Name", "Screen Rotation");

	public Field FPS_Name => A("FPS_Name", "Fps");

	public Field Language_Name => A("Language_Name", "Language");

	public Field Menu_Scale => A("Menu_Scale", "Interface Scale");

	public Field Menu_Opacity => A("Menu_Opacity", "Interface Opacity");

	public Field Shakes_Name => A("Shakes_Name", "Camera Shake");

	public Field Orbit_Line_Count => A("Orbit_Line_Count", "Orbit Line Count");

	public Field Anti_Aliasing => A("Anti_Aliasing", "Anti-Aliasing");

	[Group("Cheats")]
	public Field Infinite_Build_Area_Name => A("Infinite_Build_Area_Name", "Infinite Build Area");

	public Field Part_Clipping_Name => A("Part_Clipping_Name", "Part Clipping");

	public Field Infinite_Fuel_Name => A("Infinite_Fuel_Name", "Infinite Fuel");

	public Field No_Atmospheric_Drag_Name => A("No_Atmospheric_Drag_Name", "No Atmospheric Drag");

	public Field No_Collision_Damage_Name => A("No_Collision_Damage_Name", "No Collision Damage");

	public Field No_Gravity_Name => A("No_Gravity_Name", "No Gravity");

	public Field No_Heat_Damage_Name => A("No_Heat_Damage_Name", "No Heat Damage");

	public Field No_Burn_Marks_Name => A("No_Burn_Marks_Name", "No Burn Marks");

	[Group("Tutorials")]
	public Field Tut_Drag_And_Drop => A("Tut_Drag_And_Drop", "Drag and drop parts\nto build your rocket");

	public Field Tut_Part_Info => A("Tut_Part_Info", "Click to view\npart information");

	[LocSpace(1, false)]
	public Field Tut_Use_Part => A("Tut_Use_Part", "Click on parts to use them");

	public Field Tut_Ignition => A("Tut_Ignition", "Ignition!");

	public Field Tut_Throttle => A("Tut_Throttle", "Adjust throttle");

	[Group("Hub")]
	public Field Funds_Text => A("Funds_Text", "Funds: %funds%");

	public Field Go_To_Space_Center => A("Go_To_Space_Center", "Space Center");

	public Field Exit_To_Space_Center => A("Exit_To_Space_Center", "Exit To Space Center");

	public Field Research_And_Development => A("Research_And_Development", "Research & Development %complete%/%total%");

	public Field Challenges_Info => A("Challenges_Button", "Challenges: %complete%/%total%");

	public Field Challenges_Button => A("Challenges_Button", "Challenges %complete%/%total%");

	public Field Challenges_Title => A("Challenges_Title", "Challenges:");

	public Field Challenges_Completed => A("Challenges_Completed", "<size=55>Challenge completed:</size>\n%challenge%");

	[Group("Build")]
	public Field Build_New_Rocket => A("Build_New_Rocket", "Build New Rocket");

	public Field New => A("New", "New");

	public Field Expand_Last_Rocket => A("Expand_Last_Rocket", "Continue Build");

	[LocSpace(1, false)]
	public Field Symmetry_On => A("Symmetry_On", "Symmetry: On");

	public Field Symmetry_Off => A("Symmetry_Off", "Symmetry: Off");

	[LocSpace(1, false)]
	public Field Interior_View_On => A("Interior_View_On", "Interior View: On");

	public Field Interior_View_Off => A("Interior_View_Off", "Interior View: Off");

	[LocSpace(1, false)]
	public Field Launch_Button => A("Launch_Button", "Launch");

	public Field Move_Rocket_Button => A("Move_Rocket_Button", "Move Rocket");

	[Documentation("Clear build area", false, false)]
	public Field Clear_Warning => A("Clear_Warning", "Clear build area?");

	public Field Clear_Confirm => A("Clear_Confirm", "Clear");

	[Documentation("Launch warnings", false, false)]
	public Field Warnings_Title => A("Warnings_Title", "WARNING:");

	public Field Missing_Capsule => A("Missing_Capsule", "Your rocket has no capsule or probe, making it uncontrollable");

	[Unexported]
	public Field Mission_Crew => A("Mission_Crew", "Your rocket has no crew onboard, making it uncontrollable");

	public Field Missing_Parachute => A("Missing_Parachute", "Your rocket has no parachute");

	public Field Missing_Heat_Shield => A("Missing_Heat_Shield", "Your rocket has no heat shield");

	public Field Missing_Fuel_Popup => A("Missing_Fuel_Popup", "No fuel source");

	public Field Too_Heavy => A("Too_Heavy", Field.MultilineText("Your rocket is too heavy to launch", "%mass%", "%thrust%"));

	[LocSpace(1, false)]
	public Field Launch_Anyway_Button => A("Launch_Anyway_Button", "Launch Anyway");

	[LocSpace(1, false)]
	public Field Launch_Horizontally_Ask => A("Launch_Horizontally_Ask", "Launch horizontally?");

	public Field Launch_Horizontally_Confirm => A("Launch_Horizontally_Confirm", "Launch Horizontally");

	public Field Launch_Vertically_Confirm => A("Launch_Vertically_Confirm", "Launch Vertically");

	[Documentation("Example rockets", false, false)]
	public Field Example_Rockets_OpenMenu => A("Example_Rockets_OpenMenu", "Example Rockets");

	public Field Basic_Rocket => A("Basic_Rocket", "Basic Rocket");

	public Field Stages => A("Stages", "Two Stage Rocket");

	public Field Ideal_Stages => A("Ideal_Stages", "Three Stage Rocket");

	public Field Lander => A("Lander", "Moon Lander");

	[Group("Map")]
	public Field Toggle_Map_Button => A("Toggle_Map_Button", "Map");

	public Field Escape => A("Escape", "Escape");

	public Field Encounter => A("Encounter", "Encounter");

	public Field Rendezvous => A("Rendezvous", "Rendezvous");

	public Field Transfer => A("Transfer", "Transfer Window");

	[Group("Game")]
	public Field Throttle_On => A("Throttle_On", "On");

	public Field Throttle_Off => A("Throttle_Off", "Off");

	public Field Ignition => A("Ignition", "IGNITION");

	public Field RCS => A("RCS", "RCS");

	[Documentation("Game supports screen rotation, we split into 2 lines in vertical mode", false, false)]
	public Field Height_Terrain_Vertical => A("Height_Terrain_Vertical", "Height (Terrain):\n\n%height%");

	public Field Height_Vertical => A("Height_Vertical", "Height:\n\n%height%");

	public Field Velocity_Vertical => A("Velocity_Vertical", "Velocity:\n\n%speed%");

	public Field Velocity_Relative_Vertical => A("Velocity_Relative_Vertical", "Velocity (Relative):\n\n%speed%");

	public Field Distance_Relative_Vertical => A("Distance_Relative_Vertical", "Distance (Relative):\n\n%distance%");

	public Field Angle_Vertical => A("Angle_Vertical", "Angle:\n\n%angle% / %targetAngle%");

	[LocSpace(1, false)]
	public Field Height_Terrain_Horizontal => A("Height_Terrain_Horizontal", "Height (Terrain): %height%");

	public Field Height_Horizontal => A("Height_Horizontal", "Height: %height%");

	public Field Velocity_Horizontal => A("Velocity_Horizontal", "Velocity: %speed%");

	public Field Velocity_Relative_Horizontal => A("Velocity_Relative_Horizontal", "Velocity (Relative): %speed%");

	public Field Distance_Relative_Horizontal => A("Distance_Relative_Horizontal", "Distance (Relative): %distance%");

	public Field Angle_Horizontal => A("Angle_Horizontal", "Angle: %angle% / %targetAngle%");

	[LocSpace(1, false)]
	public Field Relative_Velocity_Arrow => A("Relative_Velocity_Arrow", "Relative Velocity\n%velocity%");

	public Field Side_Velocity_Arrow => A("Side_Velocity_Arrow", "Side Velocity\n%velocity%");

	public Field Forward_Velocity_Arrow => A("Forward_Velocity_Arrow", "Distance\n%distance%\n\nVelocity\n%velocity%");

	[Group("Failure menu")]
	public Field Failure_Cause => A("Failure_Cause", "FAILURE CAUSE:");

	public Field Failure_Crash_Into_Rocket => A("Failure_Crash_Into_Rocket", "Crashed into another rocket");

	public Field Failure_Crash_Into_Terrain => A("Failure_Crash_Into_Terrain", "Crashed into the surface of %planet{1}%");

	public Field Failure_Burn_Up => A("Failure_Burn_Up", "Burned up on reentry");

	[Group("End mission menu")]
	public Field Recover_Rocket => A("Recover_Rocket", "Recover");

	public Field Destroy_Rocket => A("Destroy_Rocket", "Destroy");

	public Field Debris_Recover => A("Debris_Recover", "Recover Debris");

	public Field Debris_Destroy => A("Debris_Destroy", "Destroy Debris");

	public Field Debris_Recover_Title => A("Debris_Recover_Title", "Recover debris?");

	public Field Debris_Destroy_Title => A("Debris_Destroy_Title", "Destroy debris?");

	public Field View_Mission_Log => A("View_Mission_Log", "View Flight Log");

	[Unexported]
	public Field Crewed_Destroy_Warning => A("Crewed_Destroy_Warning", "Destroying this rocket will kill all crew on board");

	[Documentation("Restart menu", false, false)]
	public Field Restart_Mission_To_Launch_Warning => A("Restart_Mission_To_Launch_Warning", "WARNING:\nThis will undo all progress since last launch");

	public Field Restart_Mission_To_Build_Warning => A("Restart_Mission_To_Build_Warning", "WARNING:\nThis will undo all progress since last launch");

	public Field Restart_Mission_To_Launch => A("Restart_Mission_To_Launch", "Revert To Launch");

	public Field Restart_Mission_To_Build => A("Restart_Mission_To_Build", "Revert To Build");

	public Field Revert_30_Secs => A("Revert_30_Secs", "Revert 30 Sec");

	public Field Revert_3_Min => A("Revert_3_Min", "Revert 3 Min");

	[Documentation("End mission menu", false, false)]
	public Field End_Challenges_Title => A("Challenges_Title", "Completed Challenges:");

	public Field End_Logs_Title => A("End_Logs_Title", "Mission Log:");

	public Field Continue_To_Log => A("Continue_To_Log", "Continue");

	public Field Back_To_Challenges => A("Back_To_Challenges", "Back");

	[Documentation("Clear space junk/debris", false, false)]
	public Field Clear_Debris_Warning => A("Clear_Debris_Warning", "Clear debris?\n\nThis will remove all uncontrollable rockets");

	public Field Clear_Debris_Confirm => A("Clear_Debris_Confirm", "Clear Debris");

	[Documentation("Select menu", false, false)]
	[Unexported]
	public Field Planets_Rock => A("Planets_Rock", "%planet{0}% Rock");

	[LocSpace(1, false)]
	public Field Navigate_To => A("Navigate_To", "Navigate To");

	public Field End_Navigation => A("End_Navigation", "End Navigation");

	public Field Focus => A("Focus", "Focus");

	public Field Unfocus => A("Unfocus", "Unfocus");

	public Field Track => A("Track", "Track");

	public Field Stop_Tracking => A("Stop_Tracking", "Stop Tracking");

	public Field Switch_To => A("Switch_To", "Switch To");

	[Unexported]
	public Field Collect_Rock => A("Collect_Rock", "Collect");

	[Group("Rocket")]
	public Field Default_Rocket_Name => A("Default_Rocket_Name", "Rocket");

	public Field No_Control_Msg => A("No_Control_Msg", "No control");

	[Group("Timewarp")]
	public Field Msg_Timewarp_Speed => A("Msg_Timewarp_Speed", "Time acceleration %speed%x");

	[LocSpace(1, false)]
	public Field Cannot_Timewarp_Below_Basic => A("Cannot_Timewarp_Below_Basic", "Cannot timewarp below %height%");

	public Field Cannot_Timewarp_Below => A("Cannot_Timewarp_Below", "Cannot timewarp faster than %speed%x while below %height%");

	public Field Cannot_Timewarp_While_Moving_On_Surface => A("Cannot_Timewarp_While_Moving_On_Surface", "Cannot timewarp faster than %speed%x while moving on the surface");

	public Field Cannot_Timewarp_While_Accelerating => A("Cannot_Timewarp_While_Accelerating", "Cannot timewarp faster than %speed%x while under acceleration");

	public Field Cannot_Use_Part_While_Timewarping => A("Cannot_Use_Part_While_Timewarping", "Cannot use %part{1}% while timewarping");

	public Field Cannot_Turn_While_Timewarping => A("Cannot_Turn_While_Timewarping", "Cannot turn while timewarping");

	[LocSpace(1, false)]
	public Field Timewarp_To_Button => A("Timewarp_To_Button", "Timewarp Here");

	[Group("Units")]
	public Field Thrust_To_Weight_Ratio => A("Thrust_To_Weight_Ratio", "Thrust / Weight: %value%");

	public Field Mass => A("Mass", "Mass: %value%t");

	public Field Thrust => A("Thrust", "Thrust: %value%t");

	public Field Burn_Time => A("Burn_Time", "Burn Time: %value%s");

	public Field Efficiency => A("Efficiency", "Efficiency: %value% Isp");

	public Field Mass_Unit => A("Mass_Unit", "t");

	public Field Meter_Unit => A("Meter_Unit", "m");

	public Field Km_Unit => A("Km_Unit", "km");

	public Field Meter_Per_Second_Unit => A("Meter_Per_Second_Unit", "m/s");

	[Unexported]
	public Field Mass_Title => A("Mass_Title", "Mass");

	[Unexported]
	public Field Height_Title => A("Height_Title", "Height");

	[Unexported]
	public Field Thrust_Title => A("Thrust_Title", "Thrust");

	[Unexported]
	public Field Thrust_To_Weight_Ratio_Title => A("Thrust_To_Weight_Ratio_Title", "Thrust / Weight");

	[Unexported]
	public Field Part_Count_Title => A("Part_Count_Title", "Parts");

	[Group("Timestamps")]
	public Field Second_Short => A("Second_Short", "%value%s");

	public Field Minute_Short => A("Minute_Short", "%value%m");

	public Field Hour_Short => A("Hour_Short", "%value%h");

	public Field Day_Short => A("Day_Short", "%value%d");

	[Group("Resource Types", new string[] { "lowercase" })]
	public Field Solid_Fuel => A("Solid_Fuel", "Solid fuel");

	public Field Liquid_Fuel => A("Liquid_Fuel", "Liquid fuel");

	[Unexported]
	public Field Kerolox => A("Kerolox", "Kerolox");

	[Unexported]
	public Field Hydrolox => A("Hydrolox", "Hydrolox");

	[Unexported]
	public Field Methalox => A("Methalox", "Methalox");

	[Unexported]
	public Field Hydrazine => A("Hydrazine", "Hydrazine");

	[Group("Resource Uses")]
	public Field Resource_Bars_Title => A("Resource_Bars_Title", "%resource_name{0}%:");

	public Field Info_Resource_Amount => A("Info_Resource_Amount", "%resource{0}%: %amount%");

	public Field Msg_No_Resource_Source => A("Msg_No_Resource_Source", "No %resource{1}% source");

	public Field Msg_No_Resource_Left => A("Msg_No_Resource_Left", "Out of %resource{1}%");

	[Group("Part Categories")]
	public Field Basic_Parts => A("Basic_Parts", "Basics");

	public Field Six_Wide_Parts => A("Six_Wide_Parts", "6 Wide");

	public Field Eight_Wide_Parts => A("Eight_Wide_Parts", "8 Wide");

	public Field Ten_Wide_Parts => A("Ten_Wide_Parts", "10 Wide");

	public Field Twelve_Wide_Parts => A("Twelve_Wide_Parts", "12 Wide");

	public Field Engine_Parts => A("Engine_Parts", "Engines");

	public Field Aerodynamics_Parts => A("Aerodynamics_Parts", "Aerodynamics");

	public Field Fairings_Parts => A("Fairings_Parts", "Fairings");

	public Field Structural_Parts => A("Structural_Parts", "Structural");

	public Field Other_Parts => A("Other_Parts", "Other");

	[Group("Part Names", new string[] { "lowercase" })]
	public Field Capsule_Name => A("Capsule_Name", "Capsule");

	public Field Probe_Name => A("Probe_Name", "Probe");

	public Field Parachute_Name => A("Parachute_Name", "Parachute");

	[LocSpace(1, false)]
	public Field Kolibri_Engine_Name => A("Kolibri_Engine_Name", "Kolibri Engine");

	public Field Hawk_Engine_Name => A("Hawk_Engine_Name", "Hawk Engine");

	public Field Valiant_Engine_Name => A("Valiant_Engine_Name", "Valiant Engine");

	public Field Titan_Engine_Name => A("Titan_Engine_Name", "Titan Engine");

	public Field Frontier_Engine_Name => A("Frontier_Engine_Name", "Frontier Engine");

	public Field Peregrine_Engine_Name => A("Peregrine_Engine_Name", Field.Text("Peregrine Engine"));

	public Field Ion_Engine_Name => A("Ion_Engine_Name", "Ion Engine");

	public Field RCS_Thruster_Name => A("RCS_Thruster_Name", "RCS Thruster");

	[LocSpace(1, false)]
	public Field Solid_Rocket_Booster => A("Solid_Rocket_Booster", "Solid Rocket Booster");

	[LocSpace(1, false)]
	public Field Fuel_Tank_Name => A("Fuel_Tank_Name", "Fuel Tank");

	public Field Separator_Name => A("Separator_Name", "Stage Separator");

	public Field Side_Separator_Name => A("Side_Separator_Name", "Side Separator");

	public Field Structural_Part_Name => A("Structural_Part_Name", "Structural Part");

	public Field Landing_Leg_Name => A("Landing_Leg_Name", "Landing Leg");

	public Field Aerodynamic_Nose_Cone_Name => A("Aerodynamic_Nose_Cone_Name", "Aerodynamic Nose Cone");

	public Field Aerodynamic_Fuselage_Name => A("Aerodynamic_Fuselage_Name", "Aerodynamic Fuselage");

	public Field Fairing_Name => A("Fairing_Name", "Fairing");

	public Field Rover_Wheel_Name => A("Rover_Wheel_Name", "Rover Wheel");

	public Field Docking_Port_Name => A("Docking_Port_Name", "Docking Port");

	public Field Solar_Panel_Name => A("Solar_Panel_Name", "Solar Panel");

	public Field Battery_Name => A("Battery_Name", "Battery");

	public Field RTG_Name => A("RTG_Name", "RTG");

	public Field Heat_Shield_Name => A("Heat_Shield_Name", "Heat Shield");

	public Field Fuel_Pipe_Name => A("Fuel_Pipe_Name", "Fuel Pipe");

	[Group("Part Descriptions")]
	public Field Capsule_Description => A("Capsule_Description", "A small capsule, carrying one astronaut");

	public Field Probe_Description => A("Probe_Description", "An unmanned probe, used for one way missions");

	public Field Parachute_Description => A("Parachute_Description", "A parachute used for landing");

	public Field Fuel_Tank_Description => A("Fuel_Tank_Description", "A fuel tank carrying liquid fuel and liquid oxygen");

	public Field Separator_Description => A("Separator_Description", "Vertical separator, used to detach empty stages");

	public Field Side_Separator_Description => A("Side_Separator_Description", "Horizontal separator, used for detaching side boosters");

	public Field Landing_Leg_Description => A("Landing_Leg_Description", "An extendable and retractable leg used for landing on the surface of moons and planets");

	public Field Structural_Part_Description => A("Structural_Part_Description", "A light and strong structural part");

	public Field Hawk_Engine_Description => A("Hawk_Engine_Description", "A high thrust - lower efficiency engine, normally used in the first stage of a rocket");

	public Field Titan_Engine_Description => A("Titan_Engine_Description", "A high thrust - lower efficiency engine, normally used in the first stage of a rocket");

	public Field Valiant_Engine_Description => A("Valiant_Engine_Description", "High efficiency, low thrust. Used in space when high thrust isn't a priority");

	public Field Frontier_Engine_Description => A("Frontier_Engine_Description", "High efficiency, low thrust. Used in space when high thrust isn't a priority");

	public Field Kolibri_Engine_Description => A("Kolibri_Engine_Description", "A tiny engine used for landers");

	public Field Ion_Engine_Description => A("Ion_Engine_Description", "A low thrust engine with an incredibly high efficiency");

	public Field RCS_Thruster_Description => A("RCS_Thruster_Description", "A set of small directional thrusters, used for docking");

	public Field Booster_Description => A("Booster_Description", "Has high thrust, low efficiency\nCannot be turned off or throttle once ignite");

	public Field Aerodynamic_Nose_Cone_Description => A("Aerodynamic_Nose_Cone_Description", "An aerodynamic nose cone, used to improve the aerodynamics of side boosters");

	public Field Aerodynamic_Fuselage_Description => A("Aerodynamic_Fuselage_Description", "An aerodynamic fuselage, used to cover engines");

	public Field Fairing_Description => A("Fairing_Description", "An aerodynamic fairing, used to encapsulate payloads");

	public Field Battery_Description => A("Battery_Description", "A battery used to store electric power");

	public Field Solar_Panel_Description => A("Solar_Panel_Description", "A solar panel that generates power when extended");

	public Field RTG_Description => A("RTG_Description", "Radioisotope thermoelectric generator or RTG");

	public Field Rover_Wheel_Description => A("Rover_Wheel_Description", "Rover wheel used to drive on the surface of planets");

	public Field Docking_Port_Description => A("Docking_Port_Description", "A docking port which can be used to connect two vehicles together");

	public Field Heat_Shield_Description => A("Heat_Shield_Description", "A heat resistant shield used to survive atmospheric reentry");

	public Field Fuel_Pipe_Description => A("Fuel_Pipe_Description", "A pipe used to transfer fuel");

	[Group("Modules")]
	public Field Torque_Module_Torque => A("Torque_Module_Torque", "Torque: %value%kN");

	public Field Separation_Force => A("Separation_Force", "Separation force: %value%kN");

	public Field Magnet_Force => A("Magnet_Force", "Magnet force: %value%kN");

	[LocSpace(1, false)]
	public Field Max_Heat_Tolerance => A("Max_Heat_Tolerance", "Heat tolerance: %temperature%");

	[LocSpace(1, false)]
	[MarkAsSub]
	public Field State_On => A("State_On", "On");

	[MarkAsSub]
	public Field State_Off => A("State_Off", "Off");

	[LocSpace(1, false)]
	public Field Engine_Module_State => A("Engine_Module_State", "Engine %state{0}%");

	public Field Engine_On_Label => A("Engine_On_Label", "Engine on");

	public Field Gimbal_On_Label => A("Gimbal_On_Label", "Gimbal on");

	[LocSpace(1, false)]
	public Field Msg_RCS_Module_State => A("Msg_RCS_Module_State", "RCS %state{0}%");

	[LocSpace(1, false)]
	public Field Wheel_Module_State => A("Wheel_Module_State", "Rover wheel %state{0}%");

	public Field Wheel_On_Label => A("Wheel_On_Label", "Wheel on");

	[LocSpace(1, false)]
	public Field Panel_Expanded => A("Panel_Expanded", "Expanded");

	public Field Landing_Leg_Expanded => A("Landing_Leg_Expanded", "Deployed");

	[LocSpace(1, false)]
	public Field Detach_Edges_Label => A("Detach_Edges_Label", "Detach edges");

	public Field Adapt_To_Tanks_Label => A("Adapt_To_Tanks_Label", "Adapt to fuel tanks");

	[LocSpace(1, false)]
	public Field Info_Parachute_Max_Height => A("Info_Parachute_Max_Height", "Max deploy height: %height%");

	public Field Msg_Cannot_Deploy_Parachute_In_Vacuum => A("Msg_Cannot_Deploy_Parachute_In_Vacuum", "Cannot deploy parachute in a vacuum");

	public Field Msg_Cannot_Deploy_Parachute_Above => A("Msg_Cannot_Deploy_Parachute_Above", "Cannot deploy parachute above %height%");

	public Field Msg_Cannot_Fully_Deploy_Above => A("Msg_Cannot_Fully_Deploy_Above", "Cannot fully deploy parachute above %height%");

	public Field Msg_Cannot_Deploy_Parachute_While_Faster => A("Msg_Cannot_Deploy_Parachute_While_Faster", "Cannot deploy parachute when going faster than %velocity%");

	public Field Msg_Cannot_Deploy_Parachute_While_Not_Moving => A("Msg_Cannot_Deploy_Parachute_While_Not_Moving", "Cannot deploy parachute while not moving");

	public Field Msg_Parachute_Half_Deployed => A("Msg_Parachute_Half_Deployed", "Parachute half deployed");

	public Field Msg_Parachute_Fully_Deployed => A("Msg_Parachute_Fully_Deployed", "Parachute fully deployed");

	public Field Msg_Parachute_Cut => A("Msg_Parachute_Cut", "Parachute cut");

	[Group("Planets", hasSubs = true)]
	public Field Sun => A("Sun", Field.Subs("Sun", "the Sun", "The Sun"));

	public Field Mercury => A("Mercury", "Mercury");

	public Field Venus => A("Venus", "Venus");

	public Field Earth => A("Earth", Field.Subs("Earth", "the Earth", "The Earth", "Earth's"));

	public Field Moon => A("Moon", Field.Subs("Moon", "the Moon", "The Moon"));

	public Field Near_Earth_Asteroid => A("Near_Earth_Asteroid", Field.Subs("Captured Asteroid", "the Captured Asteroid", "The Captured Asteroid"));

	public Field Mars => A("Mars", "Mars");

	public Field Phobos => A("Phobos", "Phobos");

	public Field Deimos => A("Deimos", "Deimos");

	public Field Jupiter => A("Jupiter", Field.Subs("Jupiter", "Jupiter", "Jupiter", "Jupiter's"));

	public Field Europa => A("Europa", "Europa");

	public Field Ganymede => A("Ganymede", "Ganymede");

	public Field Io => A("Io", "Io");

	public Field Callisto => A("Callisto", "Callisto");

	[Group("Landmarks")]
	public Field Sea_of_Tranquility => A("Sea_of_Tranquility", "Sea of Tranquility");

	public Field Sea_of_Serenity => A("Sea_of_Serenity", "Sea of Serenity");

	public Field Ocean_of_Storms => A("Ocean_of_Storms", "Ocean of Storms");

	public Field Copernicus_Crater => A("Copernicus_Crater", "Copernicus Crater");

	public Field Tycho_Crater => A("Tycho_Crater", "Tycho Crater");

	[LocSpace(1, false)]
	public Field Olympus_Mons => A("Olympus_Mons", "Olympus Mons");

	public Field Valles_Marineris => A("Valles_Marineris", "Valles Marineris");

	public Field Gale_Crater => A("Gale_Crater", "Gale Crater");

	public Field Hellas_Planitia => A("Hellas_Planitia", "Hellas Planitia");

	public Field Arcadia_Planitia => A("Arcadia_Planitia", "Arcadia Planitia");

	public Field Utopia_Planitia => A("Utopia_Planitia", "Utopia Planitia");

	public Field Jezero_Crater => A("Jezero_Crater", "Jezero Crater");

	[LocSpace(1, false)]
	public Field Atalanta_Planitia => A("Atalanta_Planitia", "Atalanta Planitia");

	public Field Lavinia_Planitia => A("Lavinia_Planitia", "Lavinia Planitia");

	[LocSpace(1, false)]
	public Field Caloris_Planitia => A("Caloris_Planitia", "Caloris Planitia");

	public Field Borealis_Planitia => A("Borealis_Planitia", "Borealis Planitia");

	public Field Maxwell_Montes => A("Maxwell_Montes", "Maxwell Montes");

	[Group("Challenges")]
	public Field Liftoff_Title => A("Liftoff_Title", "Liftoff");

	public Field Liftoff => A("Liftoff", "Liftoff and land safely");

	[LocSpace(1, false)]
	public Field Reach10km_Title => A("Reach10km_Title", "Reach 10km");

	public Field Reach10km => A("Reach10km", "Reach 10km and land safely");

	[LocSpace(1, false)]
	public Field ReachSpace_Title => A("ReachSpace_Title", "Reach Space");

	public Field ReachSpace => A("ReachSpace", "Reach %height%, then survive reentry and land safely");

	[LocSpace(1, false)]
	public Field Land100kmDownrange_Title => A("Land100kmDownrange_Title", "Land 100km downrange");

	public Field Land100kmDownrange => A("Land100kmDownrange", "Land at least 100km away from the launch pad");

	[LocSpace(1, false)]
	public Field ReachLowEarthOrbit_Title => A("ReachLowEarthOrbit_Title", "Reach Low Earth Orbit");

	public Field ReachLowEarthOrbit => A("ReachLowEarthOrbit", "Reach low Earth orbit, then land safely");

	[LocSpace(1, false)]
	public Field ReachHighEarthOrbit_Title => A("ReachHighEarthOrbit_Title", "Reach High Earth Orbit");

	public Field ReachHighEarthOrbit => A("ReachHighEarthOrbit", "Reach high Earth orbit, then land safely");

	[LocSpace(1, false)]
	public Field MoonOrbit_Title => A("MoonOrbit_Title", "Moon Orbit");

	public Field MoonOrbit => A("MoonOrbit", "Capture into low Moon orbit, then return safely");

	[LocSpace(1, false)]
	public Field MoonTour_Title => A("MoonTour_Title", "Moon Tour");

	public Field MoonTour => A("MoonTour", "Land on 3 separate landmarks, then return safely");

	[LocSpace(1, false)]
	public Field AsteroidImpact_Title => A("AsteroidImpact_Title", "Asteroid Impact");

	public Field AsteroidImpact => A("AsteroidImpact", "Crash into the surface of the Captured Asteroid at 200+ m/s");

	[LocSpace(1, false)]
	public Field MarsGrandTour_Title => A("MarsGrandTour_Title", "Mars Grand Tour");

	public Field MarsGrandTour => A("MarsGrandTour", "Land on Mars, Phobos and Deimos in one flight, then return safely");

	[LocSpace(1, false)]
	public Field VenusLanding_Title => A("VenusLanding_Title", "Venus Landing");

	public Field VenusLanding => A("VenusLanding", "Descend through the thick atmosphere and land on the surface of Venus");

	[LocSpace(1, false)]
	public Field VenusReturn_Title => A("VenusReturn_Title", "Venus Return");

	public Field VenusReturn => A("VenusReturn", "Land on the surface of Venus, then ascend trough the thick atmosphere and return safely");

	[LocSpace(1, false)]
	public Field MercuryLanding_Title => A("MercuryLanding_Title", "Mercury Landing");

	public Field MercuryLanding => A("MercuryLanding", "Land on the surface of Mercury");

	[LocSpace(1, false)]
	public Field MercuryReturn_Title => A("MercuryReturn_Title", "Mercury Return");

	public Field MercuryReturn => A("MercuryReturn", "Land on the surface of Mercury, then return safely");

	[LocSpace(2, false)]
	public Field LandAndReturn_Title => A("LandAndReturn_Title", "%planet{0}% Landing");

	public Field LandAndReturn => A("LandAndReturn", "Land on the surface of %planet{0}%, then return safely");

	[Group("Achievements")]
	public Field Reached_Height => A("Reached_Height", "Reached %height% altitude");

	public Field Reached_Karman_Line => A("Reached_Karman_Line", "Passed the Karman line, leaving the atmosphere and reaching space");

	public Field Survived_Reentry => A("Survived_Reentry", "Reentered %planet{3}% atmosphere, max temperature %temperature%");

	[LocSpace(1, false)]
	public Field Reached_Low_Orbit => A("Reached_Low_Orbit", "Reached low %planet{0}% orbit");

	public Field Reached_High_Orbit => A("Reached_High_Orbit", "Reached high %planet{0}% orbit");

	public Field Descend_Low_Orbit => A("Descend_Low_Orbit", "Descended to low %planet{0}% orbit");

	public Field Capture_Low_Orbit => A("Capture_Low_Orbit", "Captured into low %planet{0}% orbit");

	public Field Capture_High_Orbit => A("Capture_High_Orbit", "Captured into high %planet{0}% orbit");

	[LocSpace(1, false)]
	public Field Entered_Lower_Atmosphere => A("Entered_Lower_Atmosphere", "Entered %planet{3}% lower atmosphere");

	public Field Entered_Upper_Atmosphere => A("Entered_Upper_Atmosphere", "Entered %planet{3}% upper atmosphere");

	public Field Left_Lower_Atmosphere => A("Left_Lower_Atmosphere", "Reached %planet{3}% upper atmosphere");

	public Field Left_Upper_Atmosphere => A("Left_Upper_Atmosphere", "Escaped %planet{3}% atmosphere");

	[LocSpace(1, false)]
	public Field Landed => A("Landed", "Landed on the surface of %planet{1}%");

	public Field Landed_At_Landmark => A("Landed_At_Landmark", Field.MultilineText("Landed on the surface of %planet{1}%", "<size=55>Location: %landmark%</size>"));

	public Field Landed_At_Landmark__Short => A("Landed_At_Landmark__Short", Field.MultilineText("Landed on the surface of %planet{1}%", "- %landmark% -"));

	[LocSpace(1, false)]
	public Field Crashed_Into_Terrain => A("Crashed_Into_Terrain", "Crashed into the surface of %planet{1}%");

	[LocSpace(1, false)]
	public Field Entered_SOI => A("Entered_SOI", "Entered the sphere of influence of %planet{1}%");

	public Field Escaped_SOI => A("Escaped_SOI", "Escaped the sphere of influence of %planet{1}%");

	[LocSpace(1, false)]
	public Field Docked_Suborbital => A("Docked_Suborbital", "Docked in a suborbital trajectory of %planet{1}%");

	public Field Docked_Orbit_Low => A("Docked_Orbit_Low", "Docked in low %planet{0}% orbit");

	public Field Docked_Orbit_Transfer => A("Docked_Orbit_Transfer", "Docked in a transfer orbit of %planet{1}%");

	public Field Docked_Orbit_High => A("Docked_Orbit_High", "Docked in high %planet{0}% orbit");

	public Field Docked_Escape => A("Docked_Escape", "Docked on an escape trajectory of %planet{1}%");

	public Field Docked_Surface => A("Docked_Surface", "Docked on the surface of %planet{1}%");

	[LocSpace(1, false)]
	[Unexported]
	public Field EVA_Suborbital => A("EVA_Suborbital", "Performed a space walk in a suborbital trajectory of %planet{1}%");

	[Unexported]
	public Field EVA_Orbit_Low => A("EVA_Orbit_Low", "Performed a space walk in low %planet{0}% orbit");

	[Unexported]
	public Field EVA_Orbit_Transfer => A("EVA_Orbit_Transfer", "Performed a space walk in a transfer orbit of %planet{1}%");

	[Unexported]
	public Field EVA_Orbit_High => A("EVA_Orbit_High", "Performed a space walk in high %planet{0}% orbit");

	[Unexported]
	public Field EVA_Escape => A("EVA_Escape", "Performed a space walk on an escape trajectory of %planet{1}%");

	[Unexported]
	public Field EVA_Surface => A("EVA_Surface", "Performed a space walk on the surface of %planet{1}%");

	[Unexported]
	public Field Planted_Flag => A("Planted_Flag", "Planted a flag on the surface of %planet{1}%");

	[Unexported]
	public Field Collected_Rock => A("Collected_Rock", "Collected a rock from the surface of %planet{1}%");

	[LocSpace(1, false)]
	public Field Recover_Home => A("Recover_Home", "Safely returned to %planet{1}%");

	[Group("Mod Loader")]
	[Unexported]
	public Field Mods_Button => A("Mods_Button", "Mods");

	[Unexported]
	public Field Mod_Name => A("Mod_Name", "Mod");

	[Unexported]
	public Field AssetPack_Name => A("AssetPack_Name", "Asset Pack");

	[Unexported]
	public Field TexturePack_Name => A("TexturePack_Name", "Texture Pack");

	[Unexported]
	public Field Author_Name => A("Author_Name", "Author: {name}");

	[Unexported]
	public Field Changes_Warning => A("Changes_Warning", "Would you like to relaunch the game now so that the changes take effect?");

	[Unexported]
	public Field Disable => A("Disable", "Disable");

	[Unexported]
	public Field Enable => A("Enable", "Enable");

	[Unexported]
	public Field Relaunch => A("Relaunch", "Relaunch");

	[Group("Astronaut")]
	[Unexported]
	public Field Crew_Count => A("Crew_Count", "Crew: %count%");

	[Unexported]
	public Field Crew_Assign => A("Crew_Assign", "Assign");

	[Unexported]
	public Field Crew_Remove => A("Crew_Remove", "Remove");

	[Unexported]
	public Field EVA_Exit => A("EVA_Exit", "Exit");

	[Unexported]
	public Field EVA_Board => A("EVA_Board", "Board");

	[LocSpace(1, false)]
	[Unexported]
	public Field Crew_AwaitingMission => A("Crew_AwaitingMission", "Awaiting mission");

	[Unexported]
	public Field Crew_Available => A("Crew_Available", "Available");

	[Unexported]
	public Field Crew_Assigned => A("Crew_Assigned", "Assigned");

	[Unexported]
	public Field Crew_In_Flight => A("Crew_In_Flight", "In flight");

	[Unexported]
	public Field Crew_In_EVA => A("Crew_In_EVA", "Performing a space walk");

	[Unexported]
	public Field Crew_Deceased => A("Crew_Deceased", "Deceased");

	[LocSpace(1, false)]
	[Unexported]
	public Field Flag => A("Flag", "Flag");

	[Unexported]
	public Field Confirm_Remove_Flag => A("Confirm_Remove_Flag", "Remove flag?");

	[Unexported]
	public Field Remove_Flag => A("Remove_Flag", "Remove");

	[Unexported]
	public Field Cannot_Plant_Flag_Here => A("Cannot_Plant_Flag_Here", "Cannot plant flag here");

	[Unexported]
	public Field Cannot_Plant_Flag_Near_Another_Flag => A("Cannot_Plant_Flag_Near_Another_Flag", "Cannot plant flag near another one");

	[LocSpace(1, false)]
	[Unexported]
	public Field Astronaut_Fuel => A("Astronaut_Fuel", "Fuel");

	[Unexported]
	public Field Fuel_Running_Out => A("Fuel_Running_Out", "%percent% fuel remaining");

	[Unexported]
	public Field Out_Of_Fuel => A("Out_Of_Fuel", "Out of fuel");

	private Field A(string name, string _default)
	{
		if (fields.TryGetValue(name, out var value))
		{
			return value;
		}
		fields[name] = Field.Text(_default);
		return fields[name];
	}

	private Field A(string name, Field _default)
	{
		if (fields.TryGetValue(name, out var value))
		{
			return value;
		}
		fields[name] = _default;
		return fields[name];
	}
}
