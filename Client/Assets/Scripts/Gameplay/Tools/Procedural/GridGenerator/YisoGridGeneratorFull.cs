namespace Gameplay.Tools.Procedural.GridGenerator {
    public class YisoGridGeneratorFull: YisoGridGenerator {
        public static int[,] Generate(int width, int height, bool full) {
            var grid = PrepareGrid(ref width, ref height);
            for (var i = 0; i < width; i++) {
                for (var j = 0; j < height; j++) {
                    SetGridCoordinate(grid, i, j, full ? 1 : 0);
                }
            }
            return grid;
        }
    }
}