namespace ImageConverter
{
    public static class MathUtil
    {
        public static int IntDivisionCeil(int a, int b)
        {
            return ((a - Math.Sign(b)) / b) + 1;
        }
    }
}
