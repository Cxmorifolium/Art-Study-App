using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using artstudio.Models;
using artstudio.Models.Database;
using artstudio.Data.Repositories;

namespace artstudio.Data
{
    public class AppDatabase
    {
        private static readonly Lazy<SQLiteAsyncConnection> lazyInitializer = new Lazy<SQLiteAsyncConnection>(() =>
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "AppDatabase.db3");
            return new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);
        });

        private static SQLiteAsyncConnection Database => lazyInitializer.Value;
        private static bool initialized = false;

        static AppDatabase()
        {
            InitializeAsync().SafeFireAndForget(false);
        }

        private static async Task InitializeAsync()
        {
            if (!initialized)
            {
                if (!Database.TableMappings.Any(m => m.MappedType.Name == typeof(GalleryImage).Name))
                {
                    await Database.CreateTablesAsync(CreateFlags.None,
                        //typeof(GalleryImage),
                        //typeof(Tag),
                        //typeof(ImageTag),
                        typeof(Palette),
                        typeof(SwatchCollection),    // Add this
                        typeof(SwatchCollectionMap),
                        typeof(ColorSwatch) );
                        //typeof(WordPrompt),
                        //typeof(WordPromptTag),
                        //typeof(ImagePrompt),
                        //typeof(ImagePromptTag)).ConfigureAwait(false);
                       
                }
                initialized = true;
            }
        }

        // Repository instances  
        //private static Lazy<GalleryRepository> _galleryRepository = new Lazy<GalleryRepository>(() => new GalleryRepository(Database));
        private static Lazy<PaletteRepository> _paletteRepository = new Lazy<PaletteRepository>(() => new PaletteRepository(Database));
        //private static Lazy<WordPromptRepository> _wordPromptRepository = new Lazy<WordPromptRepository>(() => new WordPromptRepository(Database));
        //private static Lazy<ImagePromptRepository> _imagePromptRepository = new Lazy<ImagePromptRepository>(() => new ImagePromptRepository(Database));

        //public static GalleryRepository GalleryRepository => _galleryRepository.Value;
        public static PaletteRepository PaletteRepository => _paletteRepository.Value;
        //public static WordPromptRepository WordPromptRepository => _wordPromptRepository.Value;
        //public static ImagePromptRepository ImagePromptRepository => _imagePromptRepository.Value;
    }

    // Extension method for Task handling
    public static class TaskExtensions
    {
        // Safe way to fire and forget tasks without awaiting them
        public static void SafeFireAndForget(this Task task, bool continueOnCapturedContext = true, Action<Exception> onException = null)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted && onException != null)
                    onException(t.Exception);
            }, continueOnCapturedContext ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Current);
        }
    }

}
