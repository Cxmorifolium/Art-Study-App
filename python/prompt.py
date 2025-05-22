import os
import json
import random
from typing import Dict, List, Tuple, Optional, Union
from collections import defaultdict


def load_prompt_data(base_dir: str) -> Dict[str, Dict[str, List[str]]]:
    """
    Load prompt data from JSON files in the specified directory structure.
    
    Args:
        base_dir: Base directory containing category subdirectories with JSON files
        
    Returns:
        Dictionary with categories and their items
    """
    prompt_data = defaultdict(lambda: defaultdict(list))
    
    def flatten_nested_data(parent_key, value, out_dict):
        if isinstance(value, dict):
            for sub_key, sub_val in value.items():
                full_key = f"{parent_key}.{sub_key}"
                flatten_nested_data(full_key, sub_val, out_dict)
        elif isinstance(value, list):
            out_dict[parent_key].extend(value)
    
    try:
        for category in os.listdir(base_dir):
            category_path = os.path.join(base_dir, category)
            if not os.path.isdir(category_path):
                continue
                
            for filename in os.listdir(category_path):
                if not filename.endswith(".json"):
                    continue
                    
                file_path = os.path.join(category_path, filename)
                try:
                    with open(file_path, 'r', encoding='utf-8') as f:
                        data = json.load(f)
                        
                    for key, value in data.items():
                        if key in {"description", "sources"}:
                            continue
                        if isinstance(value, list):
                            prompt_data[category][key].extend(value)
                        elif isinstance(value, dict):
                            flatten_nested_data(key, value, prompt_data[category])
                except Exception as e:
                    print(f"Error reading {file_path}: {e}")
    except Exception as e:
        print(f"Error accessing directory {base_dir}: {e}")
        
    return dict(prompt_data)


class PromptGenerator:
    """
    A flexible prompt generator for creative applications that can be used with MAUI.
    Supports both default and custom generation modes.
    """
    
    def __init__(self, prompt_data: Union[Dict[str, Dict[str, List[str]]], str]):
        """
        Initialize the PromptGenerator with prompt data.
        
        Args:
            prompt_data: Either a dictionary containing prompt data or a path to the base directory
        """
        # If a string is provided, assume it's a directory path and load data
        if isinstance(prompt_data, str):
            self.prompt_data = load_prompt_data(prompt_data)
        else:
            self.prompt_data = prompt_data
            
        # Initialize with default values if certain categories don't exist
        self._ensure_default_categories()
        
    def _ensure_default_categories(self):
        """Ensure all required categories exist with at least some default values."""
        default_categories = {
            "nouns": {"general": ["object", "person", "place"]},
            "settings": {"general": ["indoors", "outdoors", "fantasy world"]},
            "styles": {"general": ["realistic", "abstract", "minimalist"]},
            "themes": {"general": ["peaceful", "chaotic", "mysterious"]}
        }
        
        for category, subcategories in default_categories.items():
            if category not in self.prompt_data:
                self.prompt_data[category] = subcategories
    
    def get_available_categories(self) -> List[str]:
        """Get a list of all available categories in the prompt data."""
        return list(self.prompt_data.keys())
    
    def get_category_items_count(self, category: str) -> int:
        """Get the total number of items available in a category across all subcategories."""
        if category not in self.prompt_data:
            return 0
            
        count = 0
        for items in self.prompt_data[category].values():
            count += len(items)
        return count
    
    def get_random_items(self, category: str, subcategory: str = None, count: int = 1) -> List[str]:
        """
        Get random items from a category and optional subcategory.
        
        Args:
            category: The category to sample from (e.g., "nouns", "settings")
            subcategory: Optional subcategory to restrict sampling
            count: Number of items to return
            
        Returns:
            List of randomly selected items
        """
        if category not in self.prompt_data:
            return []
        
        if subcategory and subcategory in self.prompt_data[category]:
            items = self.prompt_data[category][subcategory]
        else:
            # If no subcategory specified or not found, combine all subcategories
            items = []
            for sub_items in self.prompt_data[category].values():
                items.extend(sub_items)
        
        # Make sure we don't try to get more items than available
        count = min(count, len(items))
        if count == 0:
            return []
        
        return random.sample(items, count)
    
    def generate_default_prompt(self) -> Tuple[str, Dict[str, List[str]]]:
        """
        Generate a prompt using default settings:
        - Always includes 1 noun
        - 90% chance to include 1-2 settings
        - Always includes 1-2 styles
        - 70% chance to include 1 theme
        
        Returns:
            Tuple containing (prompt string, components dictionary)
        """
        return self.generate_prompt(
            include_nouns=True,
            include_settings=True,
            include_styles=True,
            include_themes=True,
            noun_count=1,
            setting_min=1,
            setting_max=2,
            style_min=1,
            style_max=2,
            theme_count=1,
            setting_probability=0.9,
            theme_probability=0.7
        )
    
    def generate_custom_prompt(self, 
                              noun_count: int = 0,
                              setting_count: int = 0,
                              style_count: int = 0,
                              theme_count: int = 0) -> Tuple[str, Dict[str, List[str]]]:
        """
        Generate a prompt with user-specified counts for each category.
        Designed to work with MAUI sliders (0-5 for nouns, 0-2 for others).
        
        Args:
            noun_count: Number of nouns to include (0-5)
            setting_count: Number of settings to include (0-2)
            style_count: Number of styles to include (0-2)
            theme_count: Number of themes to include (0-1)
            
        Returns:
            Tuple containing (prompt string, components dictionary)
        """
        # Ensure counts are within valid ranges
        noun_count = max(0, min(5, noun_count))
        setting_count = max(0, min(2, setting_count))
        style_count = max(0, min(2, style_count))
        theme_count = max(0, min(1, theme_count))
        
        return self.generate_prompt(
            include_nouns=noun_count > 0,
            include_settings=setting_count > 0,
            include_styles=style_count > 0,
            include_themes=theme_count > 0,
            noun_count=noun_count,
            setting_min=setting_count,
            setting_max=setting_count,
            style_min=style_count,
            style_max=style_count,
            theme_count=theme_count,
            setting_probability=1.0,  # Always include if count > 0
            theme_probability=1.0     # Always include if count > 0
        )
    
    def generate_prompt(self, 
                        # Category toggles
                        include_nouns: bool = True,
                        include_settings: bool = True, 
                        include_styles: bool = True,
                        include_themes: bool = True,
                        
                        # Quantity controls
                        noun_count: int = 1,
                        setting_min: int = 1,
                        setting_max: int = 2,
                        style_min: int = 1,
                        style_max: int = 2,
                        theme_count: int = 1,
                        
                        # Probability controls
                        setting_probability: float = 0.9,  # Chance to include settings at all
                        theme_probability: float = 0.5     # Chance to include themes at all
                        ) -> Tuple[str, Dict[str, List[str]]]:
        """
        Generate a random prompt with configurable components.
        
        Args:
            include_nouns: Whether to include nouns
            include_settings: Whether to include settings
            include_styles: Whether to include styles
            include_themes: Whether to include themes
            noun_count: Number of nouns to include
            setting_min: Minimum number of settings
            setting_max: Maximum number of settings
            style_min: Minimum number of styles
            style_max: Maximum number of styles
            theme_count: Number of themes to include
            setting_probability: Probability of including settings
            theme_probability: Probability of including themes
            
        Returns:
            Tuple containing (prompt string, components dictionary)
        """
        components = {}
        prompt_parts = []
        
        # Add nouns
        if include_nouns and noun_count > 0:
            nouns = self.get_random_items("nouns", count=noun_count)
            if nouns:
                components["nouns"] = nouns
                prompt_parts.extend(nouns)
        
        # Add settings
        if include_settings and random.random() < setting_probability:
            # Ensure min doesn't exceed max
            actual_setting_min = min(setting_min, setting_max)
            actual_setting_max = max(setting_min, setting_max)
            
            # If min and max are the same, use that exact number
            if actual_setting_min == actual_setting_max:
                actual_setting_count = actual_setting_min
            else:
                # Otherwise randomly choose between min and max
                actual_setting_count = random.randint(actual_setting_min, actual_setting_max)
                
            settings = self.get_random_items("settings", count=actual_setting_count)
            if settings:
                components["settings"] = settings
                prompt_parts.extend(settings)
        
        # Add styles
        if include_styles:
            # Ensure min doesn't exceed max
            actual_style_min = min(style_min, style_max) 
            actual_style_max = max(style_min, style_max)
            
            # If min and max are the same, use that exact number
            if actual_style_min == actual_style_max:
                actual_style_count = actual_style_min
            else:
                # Otherwise randomly choose between min and max
                actual_style_count = random.randint(actual_style_min, actual_style_max)
                
            styles = self.get_random_items("styles", count=actual_style_count)
            if styles:
                components["styles"] = styles
                prompt_parts.extend(styles)
        
        # Add themes (with probability)
        if include_themes and random.random() < theme_probability and theme_count > 0:
            themes = self.get_random_items("themes", count=theme_count)
            if themes:
                components["themes"] = themes
                prompt_parts.extend(themes)
        
        # Shuffle the prompt parts for variety
        random.shuffle(prompt_parts)
        prompt = ", ".join(prompt_parts)
        
        return prompt, components


# Example usage - this would be in your MAUI ViewModel
# if __name__ == "__main__":
#     # Path to your data directory containing category subdirectories
#     data_dir = "./prompt_data"
    
#     # Create generator with data from files
#     generator = PromptGenerator(data_dir)
    
#     # Generate a prompt with default settings
#     default_prompt, default_components = generator.generate_default_prompt()
#     print("Default Prompt:", default_prompt)
#     print("Components:", default_components)
    
#     # Generate a prompt with custom settings (as would come from MAUI sliders)
#     custom_prompt, custom_components = generator.generate_custom_prompt(
#         noun_count=3,      # 3 subjects
#         setting_count=1,   # 1 setting
#         style_count=2,     # 2 styles
#         theme_count=1      # 1 theme
#     )
#     print("\nCustom Prompt:", custom_prompt)
#     print("Components:", custom_components)