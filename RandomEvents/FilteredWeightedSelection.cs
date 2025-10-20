using System;
using System.Collections.Generic;
using System.Text;

namespace RandomEvents
{
    public class FilteredWeightedSelection<T> : WeightedSelection<T>
    {
        private WeightedSelection<T> baseSelection;
        private HashSet<int> ignoreIndicesSet = new HashSet<int>();

        public FilteredWeightedSelection(WeightedSelection<T> selection)
        {
            baseSelection = selection ?? throw new ArgumentNullException(nameof(selection));
        }

        public new int Count => baseSelection.Count;

        // Forward all properties and methods but intercept Evaluate and EvaluateToChoiceIndex

        public new float getTotalWeight()
        {
            return baseSelection.getTotalWeight(); // or recalc if needed
        }

        public new void AddChoice(ChoiceInfo choice)
        {
            baseSelection.AddChoice(choice);
        }

        public new void AddChoice(T value, float weight)
        {
            baseSelection.AddChoice(value, weight);
        }

        public new void RemoveChoice(int choiceIndex)
        {
            baseSelection.RemoveChoice(choiceIndex);
        }

        public new void ModifyChoiceWeight(int choiceIndex, float newWeight)
        {
            baseSelection.ModifyChoiceWeight(choiceIndex, newWeight);
        }

        public new void Clear()
        {
            baseSelection.Clear();
            ignoreIndicesSet.Clear();
        }

        public new ChoiceInfo GetChoice(int i)
        {
            return baseSelection.GetChoice(i);
        }

        public new void ForEach(Action<T, float> action)
        {
            baseSelection.ForEach(action);
        }

        public void AddIgnoreIndex(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            ignoreIndicesSet.Add(index);
        }

        public void RemoveIgnoreIndex(int index)
        {
            ignoreIndicesSet.Remove(index);
        }

        public void ClearIgnoreIndices()
        {
            ignoreIndicesSet.Clear();
        }

        // Intercepted (hidden) Evaluate method - called if used via FilteredWeightedSelection reference
        public new T Evaluate(float normalizedIndex)
        {
            int index = EvaluateToChoiceIndex(normalizedIndex);
            return baseSelection.GetChoice(index).value;
        }

        public new int EvaluateToChoiceIndex(float normalizedIndex, int[] ignoredIndices = null)
        {
            if (Count == 0)
                throw new InvalidOperationException("Cannot call Evaluate without available choices.");

            float totalWeight = 0f;
            for (int i = 0; i < Count; i++)
            {
                if (!ignoreIndicesSet.Contains(i))
                    totalWeight += baseSelection.GetChoice(i).weight;
            }

            float target = normalizedIndex * totalWeight;
            float cumulative = 0f;

            for (int i = 0; i < Count; i++)
            {
                if (!ignoreIndicesSet.Contains(i))
                {
                    cumulative += baseSelection.GetChoice(i).weight;
                    if (target < cumulative)
                        return i;
                }
            }

            for (int i = Count - 1; i >= 0; i--)
            {
                if (!ignoreIndicesSet.Contains(i))
                    return i;
            }

            throw new InvalidOperationException("No valid choices available after filtering.");
        }
    }

}
