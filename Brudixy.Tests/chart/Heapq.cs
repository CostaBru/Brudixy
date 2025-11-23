using System;
using System.Collections.Generic;

namespace Brudixy.Tests.chart
{

    public static class Heapq
    {
        // Push an element onto the heap (equivalent to Python's heappush)
        public static void Push<T>(List<T> heap, T value, Func<T, T, int> compare)
        {
            // Add the new value at the end of the list
            heap.Add(value);

            // Move the new value up to its correct position to maintain the heap property
            int index = heap.Count - 1;
            while (index > 0)
            {
                // Get the parent index
                int parentIndex = (index - 1) / 2;
                // If the current value is greater than or equal to the parent, we're done
                if (compare(heap[index], heap[parentIndex]) >= 1)
                    break;

                // Swap the current value with its parent
                Swap(heap, index, parentIndex);
                index = parentIndex; // Move up the tree
            }
        }

        // Pop the smallest element from the heap (equivalent to Python's heappop)
        public static T Pop<T>(List<T> heap, Func<T, T, int> compare)
        {
            if (heap.Count == 0)
                throw new InvalidOperationException("Heap is empty");

            // The root of the heap is the smallest element
            var result = heap[0];

            // Move the last element to the root and remove the last element
            heap[0] = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);

            // Restore the heap property by moving the new root down
            int index = 0;
            int lastIndex = heap.Count - 1;
            while (true)
            {
                int leftChild = 2 * index + 1;
                int rightChild = 2 * index + 2;
                int smallest = index;

                // Compare with the left child
                if (leftChild <= lastIndex && compare(heap[leftChild], heap[smallest]) < 0)
                    smallest = leftChild;

                // Compare with the right child
                if (rightChild <= lastIndex && compare(heap[rightChild], heap[smallest]) < 0)
                    smallest = rightChild;

                // If the smallest value is still the parent, we're done
                if (smallest == index)
                    break;

                // Swap the parent with the smallest child
                Swap(heap, index, smallest);
                index = smallest; // Move down the tree
            }

            return result;
        }

        // Swap helper function
        private static void Swap<T>(List<T> heap, int i, int j)
        {
            (heap[i], heap[j]) = (heap[j], heap[i]);
        }
    }
}