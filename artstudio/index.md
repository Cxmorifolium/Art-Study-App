# ArtStudio Documentation

*A .NET MAUI project that evolved from my inability to come up with cool themes. So why not RNG everything?*

## Intro
ArtStudio started as my journey into .NET MAUI development. This documentation serves as both a reference for the app and a showcase of what you can build while learning MAUI. 

### Concepts Explored

#### Core MAUI Concepts
- Cross-platform UI with XAML and data binding.
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
- **Accessibility**: Limited screen reader support and keyboard navication
- **Testing Coverage**: Focused on database persistence and UI binding functionality (core app requirements) rather than exhaustive unit testing of edge cases
- **Code Generation**: Manual command declarations rather than CommunityToolkit.Mvvm source generators (`[RelayCommand]`, `[ObservableProperty]` attributes; focused on understanding the underlying patterns first)
- **Code Architecture**: Some ViewModels grew larger than ideal as features expanded. 
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
-**Cross-Platform**: Currently developed for windows application, but can easily be developed for mobile application.
