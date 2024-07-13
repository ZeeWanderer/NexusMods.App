import re
import itertools

main_elements = ["DiskExists", "PrevExists", "LoadoutExists", 
                 "DiskArchived", "PrevArchived", "LoadoutArchived",
                 "PathIsIgnored"]

main_elements0 = ["DiskExists", "PrevExists", "LoadoutExists"]

conditional_elements0 = {
    "DiskExists": "DiskArchived",
    "PrevExists": "PrevArchived",
    "LoadoutExists": "LoadoutArchived"
}

def get_conditional_elements0(main: list[str]):
    return [conditional_elements0[el] for el in main]

def get_conditional_elements1(main: set[str]):
    retval = []
    if main.issuperset({"DiskExists", "PrevExists"}):
        retval.append("DiskEqualsPrev")
    if main.issuperset({"PrevExists", "LoadoutExists"}): 
        retval.append("PrevEqualsLoadout")
    if main.issuperset({"DiskExists", "LoadoutExists"}): 
        retval.append("DiskEqualsLoadout")
    
    return retval

def get_combinations(elements: list[str]):
    retval = []
    for len_ in range(1, len(elements) + 1):
        partial = [ list(x) for x in itertools.combinations(elements, len_)]
        retval.extend(partial)
    return retval

def get_main_combinations():
    return get_combinations(main_elements0)

def gen_combinations():
    retval = []
    for element in get_main_combinations():
        partial = [element]

        conditionals = get_conditional_elements0(element)
        if len(conditionals) > 1:
            conditional_combinations = get_combinations(conditionals)
            for condel in conditional_combinations:
                partial.append(element + condel)

        partial_ = partial.copy()
        for pel in partial_:
            if len(pel) > 1:
                conditionals1 = get_conditional_elements1(set(pel))
                conditional1_combinations = get_combinations(conditionals1)
                for condel in conditional1_combinations:
                    partial.append(pel + condel)

        partial_ = partial.copy()

        for pel in partial_:
            partial.append(pel + ["PathIsIgnored"])

        retval.extend(partial)
    return retval

main_combinations = get_main_combinations()
generated = gen_combinations()


def main():

    output_file_path = 'SignatureShorthand.cs'
    
    # with open(output_file_path, 'w') as file:
    #     file.writelines(output_lines)

if __name__ == "__main__":
    main()
