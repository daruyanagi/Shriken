using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Shuriken.UWP.Models
{
    public class PictureManipurationHistory : List<Picture>
    {
        const string FILENAME = "history.json";

        public async Task LoadAsync()
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(FILENAME);

                var list = await FileIO.ReadLinesAsync(file);

                this.Clear();

                foreach (var item in list)
                {
                    var f = await ApplicationData.Current.LocalFolder.GetFileAsync(item);
                    var p = await Models.Picture.FromFileAsync(f);
                    this.Add(p);
                }
            }
            catch
            {
                // Do nothing.
            }
        }

        public async Task SaveAsync()
        {
            var lines = this.Select(_ => _.FileName);
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(FILENAME, CreationCollisionOption.OpenIfExists);

            await FileIO.WriteLinesAsync(file, lines);
        }

        public async Task VacuumAsync()
        {
            this.Clear();

            var files = await ApplicationData.Current.LocalFolder.GetFilesAsync();
            foreach (var file in files) await file.DeleteAsync();
        }
    }
}
