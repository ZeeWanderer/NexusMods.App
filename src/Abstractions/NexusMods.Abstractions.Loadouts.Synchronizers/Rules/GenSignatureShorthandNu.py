import re

def summary_list(summary_line):
    summary_values = [f"{value.strip()}" for value in summary_line.split(',') if value.strip()]
    return summary_values

primary_set = { "DiskExists", "PrevExists", "LoadoutExists"}
secondary_set = { "DiskArchived", "PrevArchived", "LoadoutArchived"}
tretiary_set = { "DiskEqualsPrev", "PrevEqualsLoadout", "DiskEqualsLoadout"}

readable_name = {
    "DiskExists": "disk",
    "PrevExists": "previous state",
    "LoadoutExists": "loadout"
}

prepositions = {
    "DiskExists": "on",
    "PrevExists": "in",
    "LoadoutExists": "in"
}

secondary_to_primary = {
    "DiskArchived": "DiskExists",
    "PrevArchived": "PrevExists",
    "LoadoutArchived": "LoadoutExists"
}

tretiary_to_primary = {
    "DiskEqualsPrev": ["DiskExists", "PrevExists"],
    "PrevEqualsLoadout": ["PrevExists", "LoadoutExists"],
    "DiskEqualsLoadout": ["DiskExists", "LoadoutExists"]
}

def get_required_tretiary(main: set[str]):
    retval = []
    if main.issuperset({"DiskExists", "PrevExists"}):
        retval.append("DiskEqualsPrev")
    if main.issuperset({"PrevExists", "LoadoutExists"}): 
        retval.append("PrevEqualsLoadout")
    if main.issuperset({"DiskExists", "LoadoutExists"}): 
        retval.append("DiskEqualsLoadout")
    
    return set(retval)

def readable_str_and(list_):
    if len(list_) >= 2:
        return ', '.join(list_[:-1]) + ' and ' + list_[-1]
    else:
        return ', '.join(list_)

def readable_str_or(list_):
    if len(list_) >= 2:
        return ', '.join(list_[:-1]) + ' or ' + list_[-1]
    else:
        return ', '.join(list_)

def process_summary_list(summary: list[str]):
    set_ = set(summary)
    retval = []

    primary_present = set_.intersection(primary_set)
    primary_abscent = primary_set.difference(primary_present)

    secondary_present = set_.intersection(secondary_set)
    secondary_abscent = secondary_set.difference(secondary_present)

    tretiary_present = set_.intersection(tretiary_set)
    tretiary_required = get_required_tretiary(primary_present)
    tretiary_abscent = tretiary_required.difference(tretiary_present)

    # LINE 0
    readable_names = [ x for x in [ readable_name.get(item, "") for item in primary_present ] if x ]

    preposition = prepositions.get(list(primary_present)[0])
    if len(list(primary_present)) == 1:
        line0 = f"	/// File exists only {preposition} { readable_str_and(readable_names) }"
    else:
        line0 = f"	/// File exists {preposition} { readable_str_and(readable_names) }"

    if len(tretiary_abscent) != 0:

        if len(tretiary_abscent) == 3:
            line0 += f" but hashes for all of them are different.\n"
        elif len(tretiary_abscent) == 2:
            primary_set0_ = set(tretiary_to_primary[list(tretiary_abscent)[0]])
            primary_set1_ = set(tretiary_to_primary[list(tretiary_abscent)[1]])
            cornerstone_primary = primary_set0_.intersection(primary_set1_)
            other_primaries = primary_set0_.union(primary_set1_).difference(cornerstone_primary)

            readable_cornerstone = readable_name[cornerstone_primary.pop()]
            readable_other = [ readable_name[item] for item in other_primaries ]

            line0 += f" but hash for { readable_cornerstone } differs from {readable_str_and(readable_other)}.\n"
        else:
            readable_names = []
            all_primaries = []
            for item in tretiary_abscent:
                primaries = tretiary_to_primary[item]
                for primary in primaries:
                    if primary not in all_primaries:
                        all_primaries.append(primary)
            
            readable_names = [ x for x in [ readable_name.get(item, "") for item in all_primaries ] if x ]
            if len(all_primaries) == len(primary_present):
                if len(all_primaries) == 2:
                    line0 += f" but hashes for both of them are different.\n"
                if len(all_primaries) == 3:
                    line0 += f" but hashes for all of them are different.\n" # should be impossible to get here
            else:
                line0 += f" but hashes for { readable_str_and(readable_names) } do not match.\n"
    else:
        if len(primary_present) == 1:
            line0 += ".\n"
        elif len(primary_present) == 2:
            line0 += ". Both hashes match.\n"
        else:
            line0 += ". All hashes match.\n"

    retval.append(line0)

    # LINE 1
    line1 = ""
    if len(secondary_present) == 0:
        line1 = f"	/// File is not archived.\n"
    else:
        all_primaries = [ secondary_to_primary[secondary] for secondary in secondary_present ]
        readable_names = [ readable_name.get(item, "") for item in all_primaries ]
        if len(primary_present) == len(all_primaries):
            if len(primary_present) == 1:
                line1 = f"	/// File is archived.\n"
            if len(primary_present) == 2:
                line1 = f"	/// File is archived for both sources.\n"
            if len(primary_present) == 3:
                line1 = f"	/// File is archived for all sources.\n"
        else:
            line1 = f"	/// File is archived for {readable_str_and(readable_names)}.\n"
    retval.append(line1)
    
    if "PathIsIgnored" in set_: # LINE 2
        line2 = "	/// Path is on game-specific ingnore list.\n"
        retval.append(line2)

    return retval

def main():
    input_file_path = 'SignatureShorthand.cs'
    output_file_path = 'SignatureShorthand.cs'
    
    with open(input_file_path, 'r') as file:
        lines = file.readlines()
    
    in_summary = False
    summary_value = ""
    output_lines = []
    is_first_summary = True
    for i, line in enumerate(lines):
        if '</summary>' in line:
            in_summary = False
            if not is_first_summary:
                summary = summary_list(summary_value.strip())
                processed_summary = process_summary_list(summary)
                summary_value = ""
                output_lines.extend(processed_summary)
            else:
                is_first_summary = False
        
        elif in_summary:
            # Accumulate the summary value line(s)
            summary_value += line.replace("///","").strip() + ", "
        
        if not in_summary or is_first_summary:
            output_lines.append(line)
        
        if '<summary>' in line:
            in_summary = True
            summary_value = ""
    
    with open(output_file_path, 'w') as file:
        file.writelines(output_lines)

if __name__ == "__main__":
    main()
