# ArtStudio Documentation
*A .NET MAUI project that evolved from my inability to come up with cool themes. So why not RNG everything? (It should really be renamed to Art Study)*

Welcome! This is a .NET MAUI project I took upon myself to learn cross-platform development while solving my own creative decision paralysis.

## Table of Contents
- [What ArtStudio Does](#what-artstudio-does)
- [Core Workflow](#core-workflow)
- [Getting Started](#getting-started)
- [Technical Overview](#technical-overview)
- [Resources](#and-if-all-else-fails)

## What ArtStudio Does
ArtStudio helps decision paralysis artists, art blocks, or artists who don't want to think (like me) organize and enhance their practice sessions by providing **limited** curated inspiration. Whether you can't decide on the theme, palette, or visual reference, ArtStudio provides it all in one app!

## Core Workflow

### 1. Start a Study Session
Choose between **Quick Sketch** (30 seconds to 30 minutes) for rapid practice, or **Session mode** (30min to 3 hours) for in-depth studies. The app generates random content for each session to keep your practice varied and challenging. Sometimes it might be vague-- you're going to have to come up with something. Use that imagination juice! Alternatively, select the word prompt page, palette page, or image generation page for less rigid studies.

### 2. Get Inspired
Every session provides:
- **Visual References**: High-quality images from Unsplash to draw from
- **Color Palettes**: Mathematically harmonious color schemes using algorithms inspired by [Coolors](https://coolors.co/) (Check it out! It's really neat)
- **Word Prompts**: Curated combinations of themes, subjects, styles, and settings to spark creativity

### 3. Practice With Purpose
Use the integrated timer to stay focused. In Quick Sketch mode, the timer automatically generates new content when time expires, perfect for gesture drawing and thumbnailing sessions. Session mode gives you extended time with rich content. You do not have to use all the prompts given! Although bonus points if you do. Challenge yourself with what's in front of you!

### 4. Build Your Favorites
Found an inspiring image or perfect color scheme? Hit the heart button to save it to your personal collection! Your favorites are cached locally and can be reused in future sessions or when creating gallery entries.

### 5. Document Your Progress
After creating artwork, click the "Add" button in the Gallery to:
- Upload a photo of your finished piece
- Link them to the prompts, colors, and references you used
- Add personal notes about what you practiced and future implementation
- Tag your work for easy searching later

## Getting Started

### Prerequisites
- .NET 9.0 SDK or later
- Visual Studio 2022 (17.12+) with .NET MAUI workload installed
- Windows 11 (or Windows 10 version 1809+ for broader compatibility)

> **Note**: This project uses .NET 9. If you have .NET 8, you'll need to upgrade to .NET 9 SDK, which you can download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)

### Installation
1. **Clone the repository**
   ```bash
   git clone https://github.com/Cxmorifolium/Art-Study-App.git
   cd Art-Study-App
2. **Restore Dependencies**
    - This steps downloads all the NuGet packages
    ```
    dotnet restore
3. **Set up Unsplash API** (Required for image features)
   - Create a free account at [Unsplash Developers](https://unsplash.com/developers)
   - Create a new application to get your Access Key
   - Choose one of these methods to add your API key:

   **Option A: Environment Variable (Recommended)**
   - **Windows (Command Prompt):** `setx UNSPLASH_ACCESS_KEY "your-access-key-here"`
   - **Windows (PowerShell):** `$env:UNSPLASH_ACCESS_KEY="your-access-key-here"`
   - **Or through System Properties:** Search "Environment Variables" → New System Variable

   **Option B: User Secrets (For development)**
   ```bash
   dotnet user-secrets set "UNSPLASH_ACCESS_KEY" "your-access-key-here"
   ```

   **Option C: Direct code modification (Quick but not recommended for sharing)**
   Edit `Services/Unsplash.cs` and replace this line:
   ```csharp
   _accessKey = Environment.GetEnvironmentVariable("UNSPLASH_ACCESS_KEY")
                ?? throw new InvalidOperationException("Missing Unsplash API key. Set UNSPLASH_ACCESS_KEY as an environment variable.");
   ```
   With:
   ```csharp
   _accessKey = "your-access-key-here";
   ```
   ⚠️ **Warning:** Don't commit this change if you plan to share your code!

   > **Note:** You may need to restart Visual Studio/your terminal after setting environment variables

4. **Build and run**
    ```
    dotnet build
    dotnet run
    ```
    *Or press F5 in Visual Studio*
## Technical Overview
ArtStudio started as my journey into .NET MAUI development. This documentation serves as both a reference for the app and a showcase of what you can build while learning MAUI.

### Concepts Explored

#### Core MAUI Concepts
- Cross-platform UI with XAML and data binding
- MVVM architecture with CommunityToolkit.Mvvm
- Local data persistence using SQLite and Entity Framework
- Platform-specific services and dependency injection
- Async programming patterns throughout the UI layer

#### Features Implemented
- **Image Management**: Unsplash API integration with local caching and favorites
- **Color Palette Generation**: Mathematical color harmony algorithms inspired by Coolors
- **Content Generation**: Dynamic word prompts for categories such as Themes, Settings, Nouns, and Styles, from JSON database curated by myself and sourced from [Corpora](https://github.com/dariusk/corpora)
- **Study Sessions**: Timer functionality with session persistence
- **Data Management**: Complete CRUD operations with database migrations
- **Responsive UI**: Flyout panels, collection views, and modern design patterns

### Areas of Improvement

#### Learning Project Scope Limitations:
- **Error Handling**: Basic try-catch blocks rather than comprehensive error recovery strategies
- **Performance Optimization**: No image compression or lazy loading for large galleries
- **Cloud Integration**: Local-only storage without backup or sync capabilities
- **Accessibility**: Limited screen reader support and keyboard navigation
- **Testing Coverage**: Focused on database persistence and UI binding functionality (core app requirements) rather than exhaustive unit testing of edge cases
- **Code Generation**: Manual command declarations rather than CommunityToolkit.Mvvm source generators (`[RelayCommand]`, `[ObservableProperty]` attributes; focused on understanding the underlying patterns first)
- **Code Architecture**: Some ViewModels grew larger than ideal as features expanded
- **Memory Management**: Could benefit from better image disposal and caching strategies

#### Technical Debt Acknowledged:
- **Database Design**: Some tables could be better normalized
- **API Rate Limiting**: Basic Unsplash integration (which was good enough for the scope of the project)
- **Configuration Management**: Hard-coded values that should be configurable
- **Dependency Injection**: Could be more granular and testable

<small>*Side note: There are about 200+ compiler warnings (mostly data binding optimization suggestions), that remain unaddressed because, well... if it ain't broke—👀*</small>

#### Future Enhancements
- **Code Refactoring**: Better utilization of CommunityToolkit.Mvvm source generators, improved code separation for readability and cleaner architecture. Also investigate Windows notification display issues (snackbars work but aren't visible - low priority debugging)
- **User Experience**: Onboarding tooltips for first-time users, enhanced accessibility features, smoother loading states and page transitions
- **Advanced Features**: Machine learning-powered palette generation, user-defined prompt categories, collaborative study sessions, cloud synchronization
- **Cross-Platform**: Currently developed for Windows application, but can easily be developed for mobile application

## And If All Else Fails...
I got free resources for you!

**Practice**
- [Drawabox](https://drawabox.com/)
- [Line of Action](https://line-of-action.com/practice-tools/app#/)
- [Sketch Daily](https://www.sketchdaily.net/)

**Poses**
- [PoseMy](https://posemy.art/)
- [Just Sketch](https://justsketch.me/)
- [Pose Maniacs](https://www.posemaniacs.com/en)

**Colors**
- [Coolors](https://coolors.co/)

**Apps**
- [PureRef](https://www.pureref.com/): An amazing app to load all your references in one canvas, drag and drop style

**YouTube**
- [Proko](https://www.youtube.com/@ProkoTV)
- [Marco Bucci](https://www.youtube.com/@marcobucci)
- [Marc Brunet](https://www.youtube.com/@YTartschool)
- [Sinix Design](https://www.youtube.com/@sinixdesign) (He also runs a discord called Chroma Corp, which is an annual summer drawing showdown. Basically getting burnt out for 30ish days. #Teamwashout. Come join when it comes! Look out for a video notification via Youtube)

And then when you're stressed out:
- [Yoga with Charlie Follows](https://www.youtube.com/@CharlieFollows)

...I'm forgetting a lot, but I know you're resourceful~ You spend hours looking for references and not draw a single thing! 😉