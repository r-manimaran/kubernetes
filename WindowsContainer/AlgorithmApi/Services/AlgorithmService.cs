using System;

namespace AlgorithmApi.Services
{
    public class AlgorithmService
    {
        public int[] BubbleSort(int[] numbers)
        {
            var arr = (int[])numbers.Clone();
            for (int i = 0; i < arr.Length - 1; i++)
                for (int j = 0; j < arr.Length - i - 1; j++)
                    if (arr[j] > arr[j + 1])
                    {
                        int tmp = arr[j]; arr[j] = arr[j + 1]; arr[j + 1] = tmp;
                    }
            return arr;
        }

        public int BinarySearch(int[] numbers, int target)
        {
            var sorted = BubbleSort(numbers);
            int low = 0, high = sorted.Length - 1;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                if (sorted[mid] == target) return mid;
                if (sorted[mid] < target) low = mid + 1;
                else high = mid - 1;
            }
            return -1;
        }

        public bool IsPalindrome(string input)
        {
            var chars = input.ToCharArray();
            Array.Reverse(chars);
            return input.Equals(new string(chars), StringComparison.OrdinalIgnoreCase);
        }
    }
}
