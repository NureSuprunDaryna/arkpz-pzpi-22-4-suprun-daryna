namespace Core.Helpers
{
    public static class MappingHelper
    {
        public static void MapFrom<TGoal, TSource>(this TGoal goal, TSource source)
        {
            var sourceProps = typeof(TSource).GetProperties();
            var goalProps = typeof(TGoal).GetProperties();

            foreach (var property in typeof(TSource).GetProperties())
            {
                var goalProp = goalProps.FirstOrDefault(p => p.Name == property.Name);

                if (goalProp != null && goalProp.PropertyType == property.PropertyType)
                {
                    goalProp.SetValue(goal, property.GetValue(source));
                }
            }
        }
    }
}
