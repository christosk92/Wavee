using System;
using System.Collections.Generic;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Eum.UI.WinUI.Transitions
{
    /// <summary>
    /// Indicates a type of search for elements in a visual or logical tree.
    /// </summary>
    public enum SearchType
    {
        /// <summary>
        /// Depth-first search, where each branch is recursively explored until the end before moving to the next one.
        /// </summary>
        DepthFirst,

        /// <summary>
        /// Breadth-first search, where each depthwise level is completely explored before moving to the next one.
        /// This is particularly useful if the target element to find is known to not be too distant from the starting
        /// point and the whole visual/logical tree from the root is large enough, as it can reduce the traversal time.
        /// </summary>
        BreadthFirst
    }
    static class T
    {
        /// <summary>
        /// Find all descendant elements of the specified element (or self). This method can be chained
        /// with LINQ calls to add additional filters or projections on top of the returned results.
        /// <para>
        /// This method is meant to provide extra flexibility in specific scenarios and it should not
        /// be used when only the first item is being looked for. In those cases, use one of the
        /// available <see cref="FindDescendantOrSelf{T}(DependencyObject)"/> overloads instead,
        /// which will offer a more compact syntax as well as better performance in those cases.
        /// </para>
        /// </summary>
        /// <param name="element">The root element.</param>
        /// <returns>All the descendant <see cref="DependencyObject"/> instance from <paramref name="element"/>.</returns>
        public static IEnumerable<DependencyObject> FindDescendantsOrSelf(this DependencyObject element)
        {
            return FindDescendantsOrSelf(element, SearchType.DepthFirst);
        }
        /// <summary>
        /// Find all descendant elements of the specified element (or self). This method can be chained
        /// with LINQ calls to add additional filters or projections on top of the returned results.
        /// <para>
        /// This method is meant to provide extra flexibility in specific scenarios and it should not
        /// be used when only the first item is being looked for. In those cases, use one of the
        /// available <see cref="FindDescendantOrSelf{T}(DependencyObject)"/> overloads instead,
        /// which will offer a more compact syntax as well as better performance in those cases.
        /// </para>
        /// </summary>
        /// <param name="element">The root element.</param>
        /// <param name="searchType">The search type to use to explore the visual tree.</param>
        /// <returns>All the descendant <see cref="DependencyObject"/> instance from <paramref name="element"/>.</returns>
        public static IEnumerable<DependencyObject> FindDescendantsOrSelf(this DependencyObject element, SearchType searchType)
        {
            // Depth-first traversal, with recursion
            static IEnumerable<DependencyObject> FindDescendantsWithDepthFirstSearch(DependencyObject element)
            {
                yield return element;

                int childrenCount = VisualTreeHelper.GetChildrenCount(element);

                for (var i = 0; i < childrenCount; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(element, i);

                    yield return child;

                    foreach (DependencyObject childOfChild in child.FindDescendants())
                    {
                        yield return childOfChild;
                    }
                }
            }

            // Breadth-first traversal, with pooled local stack
            static IEnumerable<DependencyObject> FindDescendantsWithBreadthFirstSearch(DependencyObject element)
            {
                yield return element;

                using ArrayPoolBufferWriter<object> bufferWriter = ArrayPoolBufferWriter<object>.Create();

                int childrenCount = VisualTreeHelper.GetChildrenCount(element);

                for (int i = 0; i < childrenCount; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(element, i);

                    yield return child;

                    bufferWriter.Add(child);
                }

                for (int i = 0; i < bufferWriter.Count; i++)
                {
                    DependencyObject parent = (DependencyObject)bufferWriter[i];

                    childrenCount = VisualTreeHelper.GetChildrenCount(parent);

                    for (int j = 0; j < childrenCount; j++)
                    {
                        DependencyObject child = VisualTreeHelper.GetChild(parent, j);

                        yield return child;

                        bufferWriter.Add(child);
                    }
                }
            }

            static IEnumerable<DependencyObject> ThrowArgumentOutOfRangeExceptionForInvalidSearchType()
            {
                throw new ArgumentOutOfRangeException(nameof(searchType), "The input search type is not valid");
            }

            return searchType switch
            {
                SearchType.DepthFirst => FindDescendantsWithDepthFirstSearch(element),
                SearchType.BreadthFirst => FindDescendantsWithBreadthFirstSearch(element),
                _ => ThrowArgumentOutOfRangeExceptionForInvalidSearchType()
            };
        }

    }
}
